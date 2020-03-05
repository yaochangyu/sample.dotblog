using System;
using System.Linq;
using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management;
using Hangfire.MemoryStorage;
using Owin;

namespace Lab.HangfireUnitTest
{
    internal class HangfireConfig
    {
        private static readonly string DbConnectionName = "Hangfire";
        private static readonly string Url              = "/hangfire";

        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseManagementPages(cc => cc.AddJobs(() => { return GetModuleTypes(); }))
                               .UseMemoryStorage()

                               //.UseSQLiteStorage(DbConnectionName)
                               .UseConsole()
                ;

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerCheckInterval     = new TimeSpan(0, 0, 0),
                SchedulePollingInterval = new TimeSpan(0, 0, 0),
                Queues                  = new[] {"default"}
            });
            app.UseHangfireDashboard(Url);
        }

        private static Type[] GetModuleTypes()
        {
            var assemblies = new[] {Assembly.GetEntryAssembly()};
            var moduleTypes = assemblies.SelectMany(f =>
                                                    {
                                                        try
                                                        {
                                                            return f.GetTypes();
                                                        }
                                                        catch (Exception)
                                                        {
                                                            return new Type[] { };
                                                        }
                                                    }
                                                   ) /*.Where(f => f.IsClass && typeof(IClientModule).IsAssignableFrom(f))*/
                                        .ToArray();

            return moduleTypes;
        }
    }
}