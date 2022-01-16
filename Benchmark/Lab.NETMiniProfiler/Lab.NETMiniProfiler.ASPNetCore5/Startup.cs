using System.Diagnostics;
using System.Reflection;
using Lab.NETMiniProfiler.Infrastructure.EFCore5;
using Lab.NETMiniProfiler.Infrastructure.EFCore5.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Lab.NETMiniProfiler.ASPNetCore5
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

                app.UseSwagger();

                //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApplication1 v1"));
                app.UseSwaggerUI(c =>
                {
                    c.RoutePrefix = "swagger";
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    c.IndexStream = () => this.GetType()
                                              .GetTypeInfo()
                                              .Assembly
                                              .GetManifestResourceStream("Lab.NETMiniProfiler.ASPNetCore5.index.html");
                });

                app.UseMiniProfiler();
            }

            PreConnectionDb(app);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApplication1", Version = "v1" });
            });

            services.AddMiniProfiler(o => o.RouteBasePath = "/profiler")
                    .AddEntityFramework();
            services.AddAppEnvironment();
            services.AddEntityFramework();
        }

        private static void PreConnectionDb(IApplicationBuilder app)
        {
            var employeeDbContextFactory =
                app.ApplicationServices.GetService<IDbContextFactory<EmployeeDbContext>>();
            var db = employeeDbContextFactory.CreateDbContext();
            if (db.Database.CanConnect())
            {
                Debug.WriteLine("資料庫已連線");
            }
        }
    }
}