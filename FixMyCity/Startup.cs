using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FixMyCity.Startup))]
namespace FixMyCity
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
