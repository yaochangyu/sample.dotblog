using Lab.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore3
{
    public class StartupInjectionOptionsMonitor
    {
        public IConfiguration Configuration { get; }

        public StartupInjectionOptionsMonitor(IConfiguration configuration)
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
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //ª`¤J IOptions
            services.AddOptions();

            //ª`¤J IConfiguration
            services.Configure<AppSetting>(this.Configuration);
            services.Configure<Player>("Player",  this.Configuration.GetSection("Player"));
            services.Configure<Player>("Player1", this.Configuration.GetSection("Player1"));
            services.Configure<Player>("Player2", this.Configuration.GetSection("Player2"));
        }
    }
}