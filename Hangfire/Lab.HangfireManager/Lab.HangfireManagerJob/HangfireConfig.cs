using Hangfire;
using Hangfire.Console;
using Owin;

namespace Lab.HangfireManagerJob
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
    }
}