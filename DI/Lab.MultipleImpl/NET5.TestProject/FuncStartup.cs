using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NET5.TestProject.File;

namespace NET5.TestProject
{
    public class FuncStartup
    {
        public FuncStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            UseFuncName(services);
        }
        private static void UseFuncName(IServiceCollection services)
        {
            services.AddSingleton<ZipFileProvider>();
            services.AddSingleton<FileProvider>();
            services.AddSingleton<Func<string, IFileProvider>>(provider =>
                                                                   key =>
                                                                   {
                                                                       switch (key)
                                                                       {
                                                                           case "zip":
                                                                               return provider
                                                                                   .GetService<ZipFileProvider>();
                                                                           case "file":
                                                                               return provider
                                                                                   .GetService<FileProvider>();
                                                                           default:
                                                                               throw new NotSupportedException();
                                                                       }
                                                                   });
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
        }
    }
}