using System;
using System.Linq;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management;
using Lab.HangfireJob;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lab.HangfireManager.AspNetCore31
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer();
            app.UseHangfireDashboard("/hangfire",
                                     new DashboardOptions
                                     {
                                         //預設授權無法在線上環境使用 Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter
                                         Authorization  = new[] {new DashboardAuthorizationFilter()},
                                         DashboardTitle = "排程服務"

                                         //AppPath = System.Web.VirtualPathUtility.ToAbsolute("~/"),//
                                         //DisplayStorageConnectionString = false,
                                         //IsReadOnlyFunc = f => true
                                     }
                                    );

            app.UseRouting();

            app.UseEndpoints(endpoints =>
                             {
                                 endpoints.MapGet("/",
                                                  async context =>
                                                  {
                                                      await context.Response.WriteAsync("Hello World!");
                                                  });
                             });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(config => config
                                           .UseSimpleAssemblyNameTypeSerializer()
                                           .UseRecommendedSerializerSettings()
                                           .UseColouredConsoleLogProvider()
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ServerCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RecurringJobCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RetriesCount)

                                           //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.EnqueuedCountOrNull)
                                           //.UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCountOrNull)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics
                                                                       .EnqueuedAndQueueCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics
                                                                       .ScheduledCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics
                                                                       .ProcessingCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics
                                                                       .SucceededCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.DeletedCount)
                                           .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics
                                                                       .AwaitingCount)
                                           .UseSqlServerStorage(@"Data Source=(localdb)\mssqllocaldb;Initial Catalog=Hangfire;Encrypt=False;Integrated Security=True;") //from Hangfire.SqlServer
                                           .UseManagementPages(p =>
                                                                   p.AddJobs(() =>
                                                                                 GetModuleTypes())) //from Hangfire.Dashboard.Management
                                           .UseConsole()                                            //from Hangfire.Console
                                );
        }

        public static Type[] GetModuleTypes()
        {
            //var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //var moduleDirectory = System.IO.Path.Combine(baseDirectory, "Modules");
            //var assembliePaths = System.IO.Directory.GetFiles(baseDirectory, "*.dll");
            //if (System.IO.Directory.Exists(moduleDirectory))
            //    assembliePaths = assembliePaths.Concat(System.IO.Directory.GetFiles(moduleDirectory, "*.dll")).ToArray();

            //var assemblies = assembliePaths.Select(f => System.Reflection.Assembly.LoadFile(f)).ToArray();
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