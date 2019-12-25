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

            var dashboardOptions = new DashboardOptions
            {
                Authorization = new[]
                {
                    new DashboardAuthorizationFilter(), new DashboardAuthorizationFilter()
                }
            };

            app.UseHangfireDashboard("/hangfire", dashboardOptions);

            var jobServerOptions = new BackgroundJobServerOptions
            {
                WorkerCount = 1
            };
            app.UseHangfireServer();
        }
    }
}