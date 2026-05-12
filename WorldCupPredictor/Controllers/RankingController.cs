using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Dapper;
using Sendo.FileTransfer.Infracstructure;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [Authorize]
    public class RankingController : BaseController
    {       
        public async Task<ActionResult> Index()
        {
            List<RankingModel> model;
            List<PredictionCalcModel> rating;
            using (var cnn = SqlHelper.OpenConnection())
            {
                var p = new DynamicParameters();
                p.Add("@userName", System.Web.HttpContext.Current.User.Identity.Name);
                model = (await cnn.QueryAsync<RankingModel>("usp_GetRanking_New1",p,
                    commandType: CommandType.StoredProcedure)).ToList();

                rating = (await cnn.QueryAsync<PredictionCalcModel>("usp_GetPrediction",
                    commandType: CommandType.StoredProcedure)).ToList();
            }
            
            var totalResult = rating.Count();
            @ViewBag.Win = rating.Count(x => x.Point >= 3);
            @ViewBag.WinPercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Win * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.Draw = rating.Count(x => x.Point == 1);
            @ViewBag.DrawPercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Draw * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.Loose = rating.Count(x => x.Point <= 0);
            @ViewBag.LoosePercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Loose * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.CalcMoney = rating.Sum(x => x.CalcMoney);
            @ViewBag.NotPredict = rating.Count(x => x.HomeWin == false && x.AwayWin == false);

            var myRank= model.FirstOrDefault(x => x.UserName == User.Identity.Name);
            var top1Rank = model.FirstOrDefault(x=>x.RankUser==1);
            var top24Rank = model.FirstOrDefault(x => x.RankUser == 24);

            if (myRank != null)
            {
                ViewBag.MyRank = new MyRanking()
                {
                    FullName = myRank.FullName,
                    TotalPoint = myRank.TotalPoint,
                    LastPoint = myRank.LastPoint,
                    RankUser = myRank.RankUser,
                    PointNeedToRank1 = top1Rank.TotalPoint - myRank.TotalPoint,
                    PointNeedToRank24 = top24Rank == null ? 0 : top24Rank.TotalPoint - myRank.TotalPoint
                };
            }           

            return View(model.Take(100));            
        }

        public ActionResult Sponsor()
        {
            
            List<SponsorModel> model;
            List<PredictionCalcModel> rating;
            using (var cnn = SqlHelper.OpenConnection())
            {
                model = cnn.Query<SponsorModel>("usp_GetSponsor",
                    commandType: CommandType.StoredProcedure).ToList();

                rating = cnn.Query<PredictionCalcModel>("usp_GetPrediction",
                    commandType: CommandType.StoredProcedure).ToList();
            }

            var totalResult = rating.Count();
            @ViewBag.Win = rating.Count(x => x.Point >= 3);
            @ViewBag.WinPercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Win * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.Draw = rating.Count(x => x.Point == 1);
            @ViewBag.DrawPercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Draw * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.Loose = rating.Count(x => x.Point <= 0);
            @ViewBag.LoosePercent = totalResult > 0 ? (int)(Math.Round(@ViewBag.Loose * 1.0 / totalResult, 2) * 100) : 0;
            @ViewBag.CalcMoney = rating.Sum(x => x.CalcMoney);
            @ViewBag.NotPredict = rating.Count(x => x.HomeWin == false && x.AwayWin == false);

            return View(model);
        }
	}
}