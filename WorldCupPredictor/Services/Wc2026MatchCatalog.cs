using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using Newtonsoft.Json.Linq;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Services
{
    /// <summary>
    /// Loads WC2026 fixtures/results from football-data.org (token + competition code)
    /// or from a custom JSON URL (same shape as football-data "matches" array).
    /// </summary>
    public static class Wc2026MatchCatalog
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        public static async Task<(List<Wc2026MatchVm> Matches, string ErrorMessage)> GetMatchesAsync()
        {
            var cacheKey = "Wc2026_MatchList_v2";
            var cached = HttpRuntime.Cache.Get(cacheKey) as List<Wc2026MatchVm>;
            if (cached != null)
                return (cached, null);

            var seconds = Math.Max(30, int.Parse(ConfigurationManager.AppSettings["Wc2026:CacheSeconds"] ?? "120"));

            try
            {
                var json = await FetchJsonAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                    return (new List<Wc2026MatchVm>(), "Chưa cấu hình API (token hoặc URL JSON).");

                var list = ParseMatches(json);
                HttpRuntime.Cache.Insert(cacheKey, list, null, DateTime.UtcNow.AddSeconds(seconds), Cache.NoSlidingExpiration);
                return (list, null);
            }
            catch (Exception ex)
            {
                return (new List<Wc2026MatchVm>(), "Không tải được lịch trận: " + ex.Message);
            }
        }

        private static async Task<string> FetchJsonAsync()
        {
            var customUrl = (ConfigurationManager.AppSettings["Wc2026:CustomMatchesUrl"] ?? "").Trim();
            if (!string.IsNullOrEmpty(customUrl))
            {
                using (var resp = await Http.GetAsync(customUrl).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();
                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            var token = (ConfigurationManager.AppSettings["Wc2026:FootballDataToken"] ?? "").Trim();
            var code = (ConfigurationManager.AppSettings["Wc2026:CompetitionCode"] ?? "WC").Trim();
            if (string.IsNullOrEmpty(token))
                return null;

            var url = "https://api.football-data.org/v4/competitions/" + Uri.EscapeDataString(code) + "/matches";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.TryAddWithoutValidation("X-Auth-Token", token);
                using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                {
                    var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        throw new InvalidOperationException(((int)resp.StatusCode) + " " + body);
                    return body;
                }
            }
        }

        private static List<Wc2026MatchVm> ParseMatches(string json)
        {
            var root = JObject.Parse(json);
            var arr = root["matches"] as JArray;
            if (arr == null)
                throw new InvalidOperationException("JSON không có mảng 'matches'.");

            var list = new List<Wc2026MatchVm>();
            foreach (var m in arr)
            {
                var id = m["id"]?.ToString();
                if (string.IsNullOrEmpty(id))
                    continue;

                var utc = m["utcDate"]?.ToString();
                if (string.IsNullOrEmpty(utc))
                    continue;
                var kickoff = DateTime.Parse(utc, null, System.Globalization.DateTimeStyles.RoundtripKind);
                if (kickoff.Kind == DateTimeKind.Unspecified)
                    kickoff = DateTime.SpecifyKind(kickoff, DateTimeKind.Utc);

                var status = m["status"]?.ToString() ?? "SCHEDULED";
                var home = m["homeTeam"];
                var away = m["awayTeam"];
                var stage = m["stage"]?.ToString() ?? m["group"]?.ToString();

                int? hs = null, ascore = null;
                var ft = m["score"]?["fullTime"];
                if (ft != null && ft.Type != JTokenType.Null)
                {
                    if (ft["home"] != null && ft["home"].Type != JTokenType.Null)
                        hs = ft["home"].Value<int>();
                    if (ft["away"] != null && ft["away"].Type != JTokenType.Null)
                        ascore = ft["away"].Value<int>();
                }

                list.Add(new Wc2026MatchVm
                {
                    Id = id,
                    KickoffUtc = kickoff,
                    Status = status,
                    HomeName = home?["name"]?.ToString() ?? home?["shortName"]?.ToString() ?? "?",
                    AwayName = away?["name"]?.ToString() ?? away?["shortName"]?.ToString() ?? "?",
                    HomeCrestUrl = home?["crest"]?.ToString(),
                    AwayCrestUrl = away?["crest"]?.ToString(),
                    StageLabel = stage,
                    HomeScore = hs,
                    AwayScore = ascore
                });
            }

            return list.OrderBy(x => x.KickoffUtc).ToList();
        }

        public static bool IsUpcomingFilter(Wc2026MatchVm m)
        {
            if (m.IsFinished || string.Equals(m.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                return false;
            return string.Equals(m.Status, "SCHEDULED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.Status, "TIMED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.Status, "POSTPONED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
