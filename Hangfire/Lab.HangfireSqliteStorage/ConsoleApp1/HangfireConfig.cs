using Hangfire;
using Hangfire.Console;
using Hangfire.SQLite;
using Owin;

namespace ConsoleApp1
{
    internal class HangfireConfig
    {
        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseSQLiteStorage("Hangfire")
                               .UseConsole();
 
            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();
        }
    }
}