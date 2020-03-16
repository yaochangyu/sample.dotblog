using System;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Health;
using App.Metrics.Health.Builder;
using App.Metrics.Scheduling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApiCore31
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });


            var services       = app.ApplicationServices;
            var metricsRoot    = services.GetService<IMetricsRoot>();
            SetupHealth(metricsRoot);

            app.UseMetricsAllMiddleware();
            app.UseHealthAllEndpoints();
        }

        public static void SetupHealth(IMetricsRoot metricsRoot)
        {
            long threshold = 1;

            //var healthBuilder = serviceProvider.GetService<IHealthBuilder>();
            var healthBuilder = new HealthBuilder();

            var health = healthBuilder.Configuration
                                      .Configure(p =>
                                                 {
                                                     p.Enabled          = true;
                                                     p.ReportingEnabled = true;
                                                 })
                                      .Report
                                      .ToMetrics(metricsRoot)
                                      .HealthChecks.AddProcessPrivateMemorySizeCheck("Private Memory Size", threshold)
                                      .HealthChecks.AddProcessVirtualMemorySizeCheck("Virtual Memory Size", threshold)
                                      .HealthChecks.AddProcessPhysicalMemoryCheck("Working Set", threshold)
                                      .HealthChecks.AddPingCheck("google ping", "google.com", TimeSpan.FromSeconds(10))
                                      .HealthChecks.AddHttpGetCheck("github", new Uri("https://github.com/"), TimeSpan.FromSeconds(10))
                                      .Build();


            var scheduler =
                new AppMetricsTaskScheduler(TimeSpan.FromSeconds(5),
                                            async () =>
                                            {
                                                //await Task.WhenAll(metricsRoot.ReportRunner.RunAllAsync());
                                                var healthStatus =
                                                    await health.HealthCheckRunner.ReadAsync();

                                                //using (var stream = new MemoryStream())
                                                //{
                                                //    await health.DefaultOutputHealthFormatter
                                                //                .WriteAsync(stream, healthStatus);
                                                //    var result = Encoding.UTF8.GetString(stream.ToArray());
                                                //    Console.WriteLine(result);
                                                //}

                                                foreach (var reporter in health.Reporters)
                                                {
                                                    await reporter.ReportAsync(health.Options,
                                                                               healthStatus);
                                                }
                                            });
            scheduler.Start();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            services.AddControllers();
            services.AddMvc().AddMetrics();

            //var metricsRoot = serviceProvider.GetService<IMetricsRoot>();
            //ConfigureHealth(metricsRoot);
        }
    }
}