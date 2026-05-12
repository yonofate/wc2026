using Serilog;
using Serilog.Formatting.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WorldCupPredictor
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            string logtash = ConfigurationManager.AppSettings["Logtash"];
            string logname = ConfigurationManager.AppSettings["LogAppName"];
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .WriteTo.Udp(logtash, 5044, AddressFamily.InterNetwork, new ElasticsearchJsonFormatter())
                .CreateLogger().ForContext("app", logname);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            Log.Error(exception, "Unhandled exception occurred.");
            // Log the exception (e.g., using a logging framework like log4net or NLog)
            // Clear the error from the server
            Server.ClearError();

            // Redirect to a custom error page
            //Response.Redirect("~/Error/General");
        }
    }
}
