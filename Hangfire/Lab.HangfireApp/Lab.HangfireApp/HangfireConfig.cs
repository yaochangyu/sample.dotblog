using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace Lab.HangfireApp
{
    internal class HangfireConfig
    {
        public static void Register(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "HangfireLogin"
            });

            GlobalConfiguration.Configuration
                               .UseSqlServerStorage("Hangfire")
                               .UseConsole();

            IDashboardAuthorizationFilter dashboardAuthorization = null;
#if DEV
            dashboardAuthorization = new DashboardAuthorizationFilter();

#else
            dashboardAuthorization = new DashboardBasicAuthorizationFilter();
#endif
            var dashboardOptions = new DashboardOptions
            {
                Authorization = new[]
                {
                    dashboardAuthorization
                }
            };

            app.UseHangfireDashboard("/hangfire", dashboardOptions);
            app.UseHangfireServer();
        }
    }
}