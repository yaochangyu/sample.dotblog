using System.Configuration;
using Hangfire;
using Hangfire.Console;
using Owin;

namespace Lab.HangfireApp
{
    internal class HangfireConfig
    {
        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseSqlServerStorage("Hangfire")
                               .UseConsole();

            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();
        }

        public static void Register1(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseSqlServerStorage("Hangfire")
                               .UseConsole();

            var dashboardOptions = new DashboardOptions
            {
                Authorization = new[]
                {
                    new DashboardAuthorizationFilter(), new DashboardAuthorizationFilter()
                }
            };
            
            app.UseHangfireDashboard("/hangfire", dashboardOptions);
            app.UseHangfireServer();
        }
    }
}