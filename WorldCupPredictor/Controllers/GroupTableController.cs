using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Antlr.Runtime;
using Dapper;
using Sendo.FileTransfer.Infracstructure;
using Serilog;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [Authorize]
    public class GroupTableController : BaseController
    {        
        public async Task<ActionResult> Index()
        {            
            List<Match> r1;
            List<MatchCalc> r2;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@userName", null);
                p.Add("@filter", null);
                r1 = (await cnn.QueryAsync<Match>("usp_GetAllMatch",p,
                    commandType: CommandType.StoredProcedure)).ToList();

                r2 = (await cnn.QueryAsync<MatchCalc>("usp_GetAllMatchCalc",
                    commandType: CommandType.StoredProcedure)).ToList();
            }

            List<GroupTableModel> model = r1.GroupBy(x => x.StageName).Select(group => new GroupTableModel{Stage = group.Key, Matches = group.ToList()}).ToList();
            List<GroupTableModel> calcs = r2.GroupBy(x => x.StageName).Select(group => new GroupTableModel{Stage = group.Key, MatchCalcs = group.OrderByDescending(x => x.Point).ThenByDescending(x => x.GD).ToList()}).ToList();

            foreach (var m in model)
            {
                var matchCalc = calcs.FirstOrDefault(x => x.Stage == m.Stage);
                if(matchCalc == null) continue;
                
                m.MatchCalcs = matchCalc.MatchCalcs;
            }

            return View(model);
        }       

        public ActionResult ManageMatch(int? stageId=7)
        {
            if (HttpContext.User.Identity.Name == "05491" || HttpContext.User.Identity.Name == "00067")
            {
                List<MatchInStage> match = new List<MatchInStage>();
                List<Stage> stage = new List<Stage>();
                List<SelectItem> team = new List<SelectItem>();
                using (var cnn = SqlHelper.OpenConnection())
                {
                    var p = new DynamicParameters();
                    p.Add("@stageId", stageId);
                    match = cnn.Query<MatchInStage>("usp_GetAllMatchByStage", p,
                        commandType: CommandType.StoredProcedure).ToList();

                    stage = cnn.Query<Stage>("usp_GetAllStage",
                        commandType: CommandType.StoredProcedure).ToList();

                    team = cnn.Query<SelectItem>("usp_GetAllTeam",
                        commandType: CommandType.StoredProcedure).ToList();
                }

                ViewBag.Stages = stage;
                ViewBag.StageId = stageId;
                ViewBag.Team = new SelectList(team, "Id", "Name");
                return View(match);
            }
            else
            {
                return RedirectToAction("Index","Home",new { });
            }
        }

        // POST: /GroupTable/ManageMatch
        [HttpPost]
        public ActionResult ManageMatch(List<MatchInStage> matches)
        {
            if (ModelState.IsValid && HttpContext.User.Identity.Name == "05491" || HttpContext.User.Identity.Name == "00067")
            {
                foreach (var item in matches.Where(x => x.MatchId > 0 && x.HomeTeamId > 0 && x.AwayTeamId > 0))
                {
                    using (var cnn = SqlHelper.OpenConnection())
                    {
                        var userName = System.Web.HttpContext.Current.User.Identity.Name;
                        var p = new DynamicParameters();
                        p.Add("@id", item.MatchId);
                        p.Add("@homeTeamId", item.HomeTeamId);
                        p.Add("@awayTeamId", item.AwayTeamId);
                        cnn.Execute("usp_UpdateMatch", p, commandType: CommandType.StoredProcedure);
                    }
                }
                return RedirectToAction("ManageMatch");
            }


            return RedirectToAction("ManageMatch");
        }

        [HttpPost]
        public ActionResult UpdateMatch(int matchId, int home, int away)
        {
            bool result;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var userName = System.Web.HttpContext.Current.User.Identity.Name;
                var p = new DynamicParameters();
                p.Add("@matchId", matchId);
                p.Add("@homeId", home);
                p.Add("@awayId", away);
                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                cnn.Execute("usp_UpdateMatch", p, commandType: CommandType.StoredProcedure);

                result = p.Get<bool>("@result");
                if (result == true)
                {
                    Log.Information($"{userName} update match: matchId [{matchId}] - [{home}][{away}]");
                }
            }

            return Json(result);
        }
    }
}
