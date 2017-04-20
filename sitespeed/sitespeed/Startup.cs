using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(sitespeed.Startup))]
namespace sitespeed
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
