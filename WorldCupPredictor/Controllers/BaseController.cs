using Dapper;
using Sendo.FileTransfer.Infracstructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [Compress]
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Fetch the global setting from appSettings
            string allowBetMoney = ConfigurationManager.AppSettings["AllowBetMoney"];
            string allowBetStar = ConfigurationManager.AppSettings["allowBetStar"];

            // Make it available to all views
            
            ViewBag.SportTitle = ConfigurationManager.AppSettings["SportTitle"] ??"";
            ViewBag.Company = ConfigurationManager.AppSettings["Company"] ??"";

            ViewBag.AllowBetMoney = bool.Parse(allowBetMoney ?? "false");
            ViewBag.AllowBetStar = bool.Parse(allowBetStar ?? "false");
            //ViewBag.Account = new UserAccount();
            //if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
            //{
            //    var userName = System.Web.HttpContext.Current.User.Identity.Name;
            //    using (var cnn = SqlHelper.OpenConnection())
            //    {
            //        var p = new DynamicParameters();
            //        p.Add("@userName", userName);
            //        var userDta = cnn.Query<UserAccount>("usp_GetUserByUserName", p,
            //            commandType: CommandType.StoredProcedure).FirstOrDefault();
            //        if (userDta != null)
            //            ViewBag.Account = userDta;

            //    }
            //}
        }
        // GET: Base


    }
}