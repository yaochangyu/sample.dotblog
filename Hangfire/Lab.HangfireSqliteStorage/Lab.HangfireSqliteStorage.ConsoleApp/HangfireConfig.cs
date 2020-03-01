using System;
using System.Linq;
using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Management;
using Hangfire.SQLite;
using Lab.HangfireSqliteStorage.ConsoleApp.Lab.HangfireJob;
using Owin;

namespace Lab.HangfireSqliteStorage.ConsoleApp
{
    internal class HangfireConfig
    {
        private static readonly string DbConnectionName = "Hangfire";
        private static readonly string Url              = "/hangfire";

        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseDashboardMetric(DashboardMetrics.ServerCount)
                               .UseDashboardMetric(DashboardMetrics.RecurringJobCount)
                               .UseDashboardMetric(DashboardMetrics.RetriesCount)
                               .UseDashboardMetric(DashboardMetrics.EnqueuedCountOrNull)
                               .UseDashboardMetric(DashboardMetrics.FailedCountOrNull)
                               .UseDashboardMetric(DashboardMetrics.EnqueuedAndQueueCount)
                               .UseDashboardMetric(DashboardMetrics.ScheduledCount)
                               .UseDashboardMetric(DashboardMetrics.ProcessingCount)
                               .UseDashboardMetric(DashboardMetrics.SucceededCount)
                               .UseDashboardMetric(DashboardMetrics.FailedCount)
                               .UseDashboardMetric(DashboardMetrics.DeletedCount)
                               .UseDashboardMetric(DashboardMetrics.AwaitingCount)
                               .UseManagementPages(cc => cc.AddJobs(() => { return GetModuleTypes(); }))
                               .UseSQLiteStorage(DbConnectionName)
                               .UseConsole()
                ;
            var options = new BackgroundJobServerOptions
            {
                ServerName  = $"{Environment.MachineName}.{Guid.NewGuid().ToString()}",
                WorkerCount = 20,
                Queues      = new[] {"critical", "normal", "low"}
            };
            var dashboardOptions = new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[] {new DashboardAuthorizationFilter()}
            };

            app.UseHangfireDashboard(Url, dashboardOptions);

            //app.UseHangfireServer(options);
            app.UseHangfireServer();
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