using System;
using System.Linq;
using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Management;
using Lab.HangfireJob;
using Owin;

namespace Lab.HangfireManager.AspNet48
{
    internal class HangfireConfig
    {
        private static string DbConnectionName = "Hangfire";
        private static string Url = "/hangfire";

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
                               .UseSqlServerStorage(DbConnectionName)
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

            app.UseHangfireServer(options);
            app.UseHangfireServer();
        }

        private static Type[] GetModuleTypes()
        {
            //var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //var moduleDirectory = System.IO.Path.Combine(baseDirectory, "Modules");
            //var assembliePaths = System.IO.Directory.GetFiles(baseDirectory, "*.dll");
            //if (System.IO.Directory.Exists(moduleDirectory))
            //    assembliePaths = assembliePaths.Concat(System.IO.Directory.GetFiles(moduleDirectory, "*.dll")).ToArray();

            //var assemblies = assembliePaths.Select(f => System.Reflection.Assembly.LoadFile(f)).ToArray();
            //var assemblies = new[] {typeof(AnalysisJob).Assembly};
            var assemblies = new[] {typeof(DemoJob).Assembly};
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