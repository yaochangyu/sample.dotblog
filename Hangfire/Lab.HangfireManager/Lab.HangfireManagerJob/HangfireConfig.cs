using Hangfire;
using Hangfire.Console;
using Hangfire.HttpJob;
using Owin;

namespace Lab.HangfireManagerJob
{
    internal class HangfireConfig
    {
        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseSqlServerStorage("Hangfire")
                               .UseConsole()
                               .UseHangfireHttpJob()
                ;

            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();
        }
    }
}