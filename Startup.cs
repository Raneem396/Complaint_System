using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ComplaintManagementPortal2.Startup))]
namespace ComplaintManagementPortal2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
