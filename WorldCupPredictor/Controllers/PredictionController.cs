using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Dapper;
using Sendo.FileTransfer.Infracstructure;
using Serilog;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [Authorize]
    public class PredictionController : BaseController
    {        
        public async Task<ActionResult> Index(bool? filter = null)
        {
            ViewBag.Filter = filter;
            List<Match> r;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@userName", System.Web.HttpContext.Current.User.Identity.Name);
                p.Add("@filter", filter);
                r = (await cnn.QueryAsync<Match>("usp_GetAllMatch", p,
                    commandType: CommandType.StoredProcedure)).ToList();
            }

            List<PredictionModel> model = r.GroupBy(x => x.MatchDate.Date).Select(group => new PredictionModel { Date = group.Key, Matches = group.OrderBy(x => x.MatchDate).ToList() }).ToList();
            @ViewBag.AllowStar = ConfigurationManager.AppSettings["AllowStar"] == "true" ? true : false;
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Predict(int matchId, int type)
        {
            int result;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var userName = System.Web.HttpContext.Current.User.Identity.Name;
                var p = new DynamicParameters();
                p.Add("@matchId", matchId);
                p.Add("@userName", userName);
                p.Add("@predictAction", type);
                p.Add("@result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await cnn.ExecuteAsync("usp_UpdateUserPredictionWithWarning", p, commandType: CommandType.StoredProcedure);

                result = p.Get<int>("@result");
                if (result == 1)
                {
                    string types = type == 1 ? "Home" : "Away";
                    Log.Information($"{userName} predict: matchId [{matchId}] - [{types}]");
                }                
            }

            return Json(result);
        }

        [HttpPost]
        public ActionResult Hope(int matchId, int type)
        {
            bool result=false;
            string allowStar = ConfigurationManager.AppSettings["AllowStar"];
            int maxStar = int.Parse(ConfigurationManager.AppSettings["MaxStar"] ?? "3");
            if (allowStar != "true")
            {
                return Json(new { code = 0, msg = $"Ngôi sao hy vọng chưa được áp dụng cho vòng này!" });
            }

            if (type == 1)
            {
                using (var cnn = SqlHelper.OpenConnection())
                {
                    var p = new DynamicParameters();
                    p.Add("@userName", System.Web.HttpContext.Current.User.Identity.Name);
                    var starRes = cnn.Query<StarSummaryModel>("usp_CountUsedStar", p,
                        commandType: CommandType.StoredProcedure).FirstOrDefault();
                    if (starRes != null && type == 1 && starRes.Used >= maxStar)
                    {
                        return Json(new { code = 0, msg = $"Bạn đã sử dụng hết {maxStar} ngôi sao hy vọng!" });
                    }
                }
            }

            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@matchId", matchId);
                p.Add("@userName", System.Web.HttpContext.Current.User.Identity.Name);
                p.Add("@hopeAction", type);
                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                cnn.Execute("usp_UpdateUserHope", p, commandType: CommandType.StoredProcedure);

                result = p.Get<bool>("@result");

                if (result == true && (type ==1 || type==3))
                {
                    var userName = System.Web.HttpContext.Current.User.Identity.Name;
                    string types = type == 1 ? "set" : "remove";
                    Log.Information($"{userName} {types} lucky star: matchId [{matchId}]");
                }
            }

            return Json(new { code = 1, msg = $"OK" });
        }
        
        public async Task<ActionResult> Detail(int id)
        {
            var predictDetailModel = new PredictDetailModel();
            var userCount = 0;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@matchId", id);
                p.Add("@userName", System.Web.HttpContext.Current.User.Identity.Name);
                predictDetailModel.Match = (await cnn.QueryAsync<Match>("usp_GetMatchById", p,                    
                    commandType: CommandType.StoredProcedure)).FirstOrDefault();

                var p1 = new DynamicParameters();
                p1.Add("@matchId", id);
                p1.Add("@type", 1);
                predictDetailModel.HomeWins = (await cnn.QueryAsync<ChoosingUser>("usp_GetUserByMatchId", p1,
                    commandType: CommandType.StoredProcedure)).ToList();

                var p2 = new DynamicParameters();
                p2.Add("@matchId", id);
                p2.Add("@type", 2);
                predictDetailModel.AwayWins = (await cnn.QueryAsync<ChoosingUser>("usp_GetUserByMatchId", p2,
                    commandType: CommandType.StoredProcedure)).ToList();

                var p3 = new DynamicParameters();
                p3.Add("@matchId", id);
                p3.Add("@type", 0);
                predictDetailModel.NotPredicts = (await cnn.QueryAsync<ChoosingUser>("usp_GetUserByMatchId", p3,
                    commandType: CommandType.StoredProcedure)).ToList();

                var p4 = new DynamicParameters();
                p4.Add("@matchId", id);
                userCount = (await cnn.QueryAsync<int>("usp_GetAllUserPredictedtMatch", p4, commandType: CommandType.StoredProcedure)).FirstOrDefault();
            }

            @ViewBag.HomeWin = Math.Round(predictDetailModel.HomeWins.  Count*1.0/userCount, 2)*100;
            @ViewBag.NotPredict = Math.Round(predictDetailModel.NotPredicts.Count*1.0/userCount, 2)*100;
            @ViewBag.AwayWin = Math.Round(predictDetailModel.AwayWins.Count*1.0/userCount, 2)*100;
            @ViewBag.AllowStar = ConfigurationManager.AppSettings["AllowStar"]=="true"?true:false;
            return View(predictDetailModel);
        }

        public new async Task<ActionResult> User(string userName)
        {
            var isMe = (System.Web.HttpContext.Current.User.Identity.Name == userName || string.IsNullOrEmpty(userName) == true);
            if (string.IsNullOrEmpty(userName)) userName = System.Web.HttpContext.Current.User.Identity.Name;
            var userPredictModel = new UserPredictModel()
            {
                UserName = userName
            };

            var userPredicts = new List<UserPredict>();
            var userAccount = new UserAccount();
            var starSummary = new StarSummaryModel();
            string maxStar = ConfigurationManager.AppSettings["MaxStar"]??"3";
            starSummary.Total = int.Parse(maxStar);
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();

                p.Add("@userName", userName);
                userAccount = (await cnn.QueryAsync<UserAccount>("usp_GetUserByUserName", p,
                    commandType: CommandType.StoredProcedure)).FirstOrDefault();

                p.Add("@userName", userName);
                userPredicts = (await cnn.QueryAsync<UserPredict>("usp_GetAllMatchByUserId", p,
                    commandType: CommandType.StoredProcedure)).ToList();

                var starRes = (await cnn.QueryAsync<StarSummaryModel>("usp_CountUsedStar", p,
                    commandType: CommandType.StoredProcedure)).FirstOrDefault();
                if (starRes != null) starSummary = starRes;
            }
            
            userPredictModel.UserPredicts = isMe==true? userPredicts : userPredicts.Where(x=>x.IsExpire==true).ToList();

            var totalResult = userPredicts.Count(x => x.IsExpire);
            userPredictModel.Win = userPredicts.Count(x => x.IsWin);
            userPredictModel.WinPercent = totalResult > 0 ? (int)(Math.Round(userPredictModel.Win*1.0/totalResult, 2)*100) : 0; 
            userPredictModel.Draw = userPredicts.Count(x => x.IsDraw);
            userPredictModel.DrawPercent = totalResult > 0 ? (int)(Math.Round(userPredictModel.Draw * 1.0 / totalResult, 2) * 100) : 0; 
            userPredictModel.Loose = userPredicts.Count(x => x.IsLoose);
            userPredictModel.LoosePercent = totalResult > 0 ? (int)(Math.Round(userPredictModel.Loose * 1.0 / totalResult, 2) * 100) : 0;
            userPredictModel.CalcMoney = userPredicts.Sum(x => x.CalcMoney);
            userPredictModel.NotPredict = userPredicts.Count(x => x.HomeWin == false && x.AwayWin == false);
            userPredictModel.StarSummay = starSummary;
            userPredictModel.UserAccount = userAccount;

            return View(userPredictModel);
        }

        /// <summary>
        /// Cập nhật tỉ số dự đoán
        /// </summary>
        /// <param name="matchId"></param>
        /// <param name="homeGoal"></param>
        /// <param name="awayGoal"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SetScorePreditor(int matchId, int homeGoal, int awayGoal)
        {
            int result;
            using (var cnn = SqlHelper.OpenConnection())
            {
                try
                {
                    var p = new DynamicParameters();
                    p.Add("@matchId", matchId);
                    p.Add("@userName", HttpContext.User.Identity.Name);
                    p.Add("@homeGoal", homeGoal);
                    p.Add("@awayGoal", awayGoal);
                    p.Add("@result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await cnn.ExecuteAsync("usp_UpdateScorePreditorWithWarning", p, commandType: CommandType.StoredProcedure);

                    result = p.Get<int>("@result");

                    if (result == 1)
                    {
                        var userName = System.Web.HttpContext.Current.User.Identity.Name;
                        string types = $"{homeGoal}-{awayGoal}";
                        Log.Information($"{userName} set score: matchId [{matchId}] - [{types}]");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SetScorePreditor failed for match {MatchId}", matchId);
                    return Json(0);
                }
            }

            return Json(result);
        }
        [HttpPost]
        public async Task<ActionResult> SetScrore(int matchId, int homeGoal, int awayGoal)
        {
           if (HttpContext.User.Identity.Name != "05491" && HttpContext.User.Identity.Name != "00067" && HttpContext.User.Identity.Name != "05410") return Json(false);

            bool result;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@matchId", matchId);
                p.Add("@homeGoal", homeGoal);
                p.Add("@awayGoal", awayGoal);
                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                await cnn.ExecuteAsync("usp_UpdateScore", p, commandType: CommandType.StoredProcedure);

                result = p.Get<bool>("@result");                
            }

            return Json(result);
        }

        [HttpPost]
        public ActionResult SetHandicap(int matchId, float homeHandicap, float awayHandicap)
        {
           if (HttpContext.User.Identity.Name != "05491" && HttpContext.User.Identity.Name != "00067" && HttpContext.User.Identity.Name != "05410") return Json(false);

            bool result;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@matchId", matchId);
                p.Add("@homeHandicap", homeHandicap);
                p.Add("@awayHandicap", awayHandicap);
                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                cnn.Execute("usp_UpdateHandicap", p, commandType: CommandType.StoredProcedure);

                result = p.Get<bool>("@result");
            }

            return Json(result);
        }
        
//        public ActionResult ForceUser(string userName)
//        {
//            if (HttpContext.User.Identity.Name != "05491") return Json(false);

//            bool result;
//            using (var cnn = SqlHelper.OpenConnection())
//            {
//                cnn.Execute(@"INSERT  INTO dbo.Prediction ( UserName, MatchId, HomeWin, AwayWin, Point,
//                                                  UpdatedDate, CalcMoney, GetBackMoney, IsStar,
//                                                  IsDollar )
//                            SELECT  @userName, MatchId, 0, 0, 0, GETDATE(), 20000, 0, 0, 0
//                            FROM    dbo.Prediction p
//                                    JOIN dbo.Match m ON p.MatchId = m.Id
//                            WHERE   MatchId IN ( SELECT MatchId
//                                                 FROM   dbo.Prediction
//                                                 WHERE  UserName = 'LamNH' ) AND MatchId NOT IN (
//                                    SELECT  MatchId
//                                    FROM    dbo.Prediction
//                                    WHERE   UserName = @username ) AND m.IsExpire = 1
//                            UNION
//                            SELECT  @userName, MatchId, 0, 0, 0, GETDATE(), 0, 0, 0, 0
//                            FROM    dbo.Prediction p
//                                    JOIN dbo.Match m ON p.MatchId = m.Id
//                            WHERE   MatchId IN ( SELECT MatchId
//                                                 FROM   dbo.Prediction
//                                                 WHERE  UserName = 'LamNH' ) AND MatchId NOT IN (
//                                    SELECT  MatchId
//                                    FROM    dbo.Prediction
//                                    WHERE   UserName = @username ) AND m.IsExpire = 0", new { userName });
                
//            }

//            return Json(true);
//        }
	}
}