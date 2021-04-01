using Lab.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace AspNetCore5
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
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCore5 v1"));
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
            services.AddSwaggerGen(c =>
                                   {
                                       c.SwaggerDoc("v1", new OpenApiInfo {Title = "AspNetCore5", Version = "v1"});
                                   });

            //驗證 AppSetting
            services.AddOptions<AppSetting>()
                    .ValidateDataAnnotations()
                    .Validate(p =>
                              {
                                  if (p.AllowedHosts == null)
                                  {
                                      return false;
                                  }

                                  return true;
                              }, "AllowedHosts must be value"); // Failure message.

            //注入 Options 和完整 IConfiguration
            services.Configure<AppSetting>(this.Configuration);
            
            //注入 Options 和 Configuration Section Name
            services.Configure<Player>("Player1",           this.Configuration.GetSection("Player1"));
            services.Configure<Player>("Player2",           this.Configuration.GetSection("Player2"));
            services.Configure<Player>("Player3",           this.Configuration.GetSection("Player3"));
            services.Configure<Player>("ConnectionStrings", this.Configuration.GetSection("ConnectionStrings"));
            // services.PostConfigure<Player>("Player1", config =>
            //                                          {
            //                                              config.AppId = "post_configured_option1_value";
            //                                          });
            // services.PostConfigureAll<AppSetting>(config =>
            //                                       {
            //                                           config.Player.AppId = "post_configured_option1_value";
            //                                       });
        }
    }
}