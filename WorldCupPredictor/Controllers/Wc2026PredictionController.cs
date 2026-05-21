using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Serilog;
using WorldCupPredictor.Models;
using WorldCupPredictor.Services;

namespace WorldCupPredictor.Controllers
{
    [Authorize]
    public class Wc2026PredictionController : BaseController
    {
        private readonly Wc2026PredictionRepository _repo = new Wc2026PredictionRepository();

        private static void AttachUserPicks(IEnumerable<Wc2026MatchVm> matches, IReadOnlyList<Wc2026StoredPrediction> preds)
        {
            var map = preds.ToDictionary(x => x.ExternalMatchId, StringComparer.OrdinalIgnoreCase);
            foreach (var m in matches)
            {
                if (!map.TryGetValue(m.Id, out var p))
                    continue;
                m.UserHomeWin = p.HomeWin;
                m.UserAwayWin = p.AwayWin;
                m.UserHomeGoalPre = p.HomeGoalPre;
                m.UserAwayGoalPre = p.AwayGoalPre;
            }
        }

        public async Task<ActionResult> Index(bool? filter = null)
        {
            ViewBag.Filter = filter;
            var userName = User.Identity.Name;

            var catalog = await Wc2026MatchCatalog.GetMatchesAsync();
            var matches = catalog.Matches;
            ViewBag.ApiError = catalog.ErrorMessage;

            IReadOnlyList<Wc2026StoredPrediction> preds = Array.Empty<Wc2026StoredPrediction>();
            try
            {
                preds = await _repo.GetByUserAsync(userName);
            }
            catch (Exception ex)
            {
                ViewBag.DbError = "Không đọc được bảng dự đoán WC2026 (đã chạy script SQL?): " + ex.Message;
            }

            IEnumerable<Wc2026MatchVm> q = matches;
            if (filter == true)
                q = matches.Where(Wc2026MatchCatalog.IsUpcomingFilter);

            AttachUserPicks(q, preds);

            var model = q.GroupBy(x => x.KickoffUtc.Date)
                .Select(g => new Wc2026DayGroup { Date = g.Key, Matches = g.OrderBy(x => x.KickoffUtc).ToList() })
                .ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Predict(string matchId, int type)
        {
            var catalog = await Wc2026MatchCatalog.GetMatchesAsync();
            var m = catalog.Matches.FirstOrDefault(x => string.Equals(x.Id, matchId, StringComparison.OrdinalIgnoreCase));
            if (m == null || m.IsPredictionClosed)
                return Json(0);

            var userName = User.Identity.Name;
            var home = type == 1;
            var away = type == 2;
            try
            {
                await _repo.UpsertPickAsync(matchId, userName, home, away);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WC2026 Predict failed");
                return Json(0);
            }
            Log.Information("{User} wc2026 predict match {Match} type {Type}", userName, matchId, type);
            return Json(1);
        }

        [HttpPost]
        public async Task<ActionResult> SetScore(string matchId, int homeGoal, int awayGoal)
        {
            var catalog = await Wc2026MatchCatalog.GetMatchesAsync();
            var m = catalog.Matches.FirstOrDefault(x => string.Equals(x.Id, matchId, StringComparison.OrdinalIgnoreCase));
            if (m == null || m.IsPredictionClosed)
                return Json(0);

            if (homeGoal < 0 || homeGoal > 9 || awayGoal < 0 || awayGoal > 9)
                return Json(0);

            var userName = User.Identity.Name;
            Wc2026StoredPrediction row;
            try
            {
                row = await _repo.GetAsync(matchId, userName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WC2026 SetScore read failed");
                return Json(0);
            }

            if (row == null || (!row.HomeWin && !row.AwayWin))
                return Json(0);

            var predictHome = row.HomeWin;
            var homeWinsGame = homeGoal > awayGoal;
            var awayWinsGame = awayGoal > homeGoal;
            if (predictHome && awayWinsGame)
                return Json(2);
            if (!predictHome && row.AwayWin && homeWinsGame)
                return Json(2);
            if (homeGoal == awayGoal)
                return Json(2);

            try
            {
                await _repo.UpsertScoreAsync(matchId, userName, homeGoal, awayGoal);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WC2026 SetScore save failed");
                return Json(0);
            }
            Log.Information("{User} wc2026 score match {Match} {H}-{A}", userName, matchId, homeGoal, awayGoal);
            return Json(1);
        }

        public async Task<ActionResult> Detail(string id)
        {
            var catalog = await Wc2026MatchCatalog.GetMatchesAsync();
            ViewBag.ApiError = catalog.ErrorMessage;
            var m = catalog.Matches.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (m == null)
                return HttpNotFound();

            var userName = User.Identity.Name;
            IReadOnlyList<Wc2026StoredPrediction> preds = Array.Empty<Wc2026StoredPrediction>();
            IReadOnlyList<Wc2026StoredPrediction> all = Array.Empty<Wc2026StoredPrediction>();
            try
            {
                preds = await _repo.GetByUserAsync(userName);
                all = await _repo.GetByMatchAsync(id);
            }
            catch (Exception ex)
            {
                ViewBag.DbError = "Không đọc được bảng dự đoán WC2026 (đã chạy script SQL?): " + ex.Message;
            }

            AttachUserPicks(new[] { m }, preds);

            var homeC = all.Count(x => x.HomeWin);
            var awayC = all.Count(x => x.AwayWin);
            var total = homeC + awayC;

            var vm = new Wc2026DetailVm
            {
                Match = m,
                HomePickCount = homeC,
                AwayPickCount = awayC,
                TotalPickCount = total
            };
            ViewBag.HomePct = total > 0 ? Math.Round(homeC * 100.0 / total, 0) : 0;
            ViewBag.AwayPct = total > 0 ? Math.Round(awayC * 100.0 / total, 0) : 0;
            return View(vm);
        }

        public async Task<ActionResult> Results(string userName)
        {
            var isMe = User.Identity.Name == userName || string.IsNullOrEmpty(userName);
            if (string.IsNullOrEmpty(userName))
                userName = User.Identity.Name;

            var catalog = await Wc2026MatchCatalog.GetMatchesAsync();
            ViewBag.ApiError = catalog.ErrorMessage;
            var matchMap = catalog.Matches
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var preds = new List<Wc2026StoredPrediction>();
            try
            {
                preds = (await _repo.GetByUserAsync(userName)).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.DbError = "Không đọc được bảng dự đoán WC2026 (đã chạy script SQL?): " + ex.Message;
            }
            var rows = new List<Wc2026UserRowVm>();
            var no = 1;
            foreach (var p in preds.OrderBy(x =>
                matchMap.TryGetValue(x.ExternalMatchId, out var mm) ? mm.KickoffUtc : DateTime.MaxValue))
            {
                if (!matchMap.TryGetValue(p.ExternalMatchId, out var m))
                    continue;

                var row = new Wc2026UserRowVm
                {
                    No = no++,
                    MatchId = m.Id,
                    HomeName = m.HomeName,
                    AwayName = m.AwayName,
                    HomeWin = p.HomeWin,
                    AwayWin = p.AwayWin,
                    HomeGoalPre = p.HomeGoalPre,
                    AwayGoalPre = p.AwayGoalPre,
                    HomeGoal = m.HomeScore,
                    AwayGoal = m.AwayScore,
                    IsExpire = m.IsFinished
                };
                if (!isMe && !row.IsExpire)
                    continue;
                rows.Add(row);
            }

            var settled = rows.Where(x => x.IsExpire).ToList();
            var totalR = settled.Count;
            var win = totalR > 0 ? settled.Count(x => x.IsWin) : 0;
            var draw = totalR > 0 ? settled.Count(x => x.IsDraw) : 0;
            var loose = totalR > 0 ? settled.Count(x => x.IsLoose) : 0;
            var vm = new Wc2026UserSummaryVm
            {
                UserName = userName,
                Win = win,
                Draw = draw,
                Loose = loose,
                WinPercent = totalR > 0 ? (int)Math.Round(win * 100.0 / totalR, 0) : 0,
                DrawPercent = totalR > 0 ? (int)Math.Round(draw * 100.0 / totalR, 0) : 0,
                LoosePercent = totalR > 0 ? (int)Math.Round(loose * 100.0 / totalR, 0) : 0,
                NotPredict = preds.Count(x => !x.HomeWin && !x.AwayWin),
                Rows = rows
            };

            return View("User", vm);
        }
        //public ActionResult User(int id)
        //{
        //    return View();
        //}
    }
}
