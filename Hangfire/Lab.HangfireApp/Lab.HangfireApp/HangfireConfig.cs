using Hangfire;
using Hangfire.Console;
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

            var dashboardOptions = new DashboardOptions
            {
                Authorization = new[]
                {
                    new DashboardBasicAuthorizationFilter()
                }
            };

            app.UseHangfireDashboard("/hangfire", dashboardOptions);
            app.UseHangfireServer();
        }
    }
}