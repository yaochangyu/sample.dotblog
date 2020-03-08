using System;
using System.Linq;
using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Lab.HangfireServer.Filters;
using Owin;

namespace Lab.HangfireServer
{
    internal class HangfireConfig
    {
        private static readonly string Url              = "/hangfire";
        private static          string DbConnectionName = "Hangfire";

        public static void Register(IAppBuilder app)
        {
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 5 });
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
                               //.UseSqlServerStorage(DbConnectionName)
                               .UseMemoryStorage(new MemoryStorageOptions
                               {
                                   FetchNextJobTimeout = new TimeSpan(0, 0, 5)
                               })
                               .UseConsole()
                ;
            var options = new BackgroundJobServerOptions
            {
                WorkerCount = 20,
                Queues      = new[] {"default", "critical", "normal", "low"}
            };
            var dashboardOptions = new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[] {new DashboardAuthorizationFilter()}
            };

            app.UseHangfireDashboard(Url, dashboardOptions);

            app.UseHangfireServer(options);

            //app.UseHangfireServer();
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
            var assemblies = new[] {Assembly.GetExecutingAssembly()};
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