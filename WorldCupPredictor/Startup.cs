using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Formatting.Elasticsearch;
using System.Configuration;
using System.Net.Sockets;

[assembly: OwinStartupAttribute(typeof(WorldCupPredictor.Startup))]
namespace WorldCupPredictor
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
