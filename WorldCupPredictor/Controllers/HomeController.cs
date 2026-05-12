using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace WorldCupPredictor.Controllers
{    
    public class HomeController : BaseController
    {
        //[OutputCache(Duration = 60, VaryByParam = "none")]
        public async Task<ActionResult> Index()
        {
            return View();
        }

        public async Task<ActionResult> Comment()
        {            
            return View();
        }        
    }
}