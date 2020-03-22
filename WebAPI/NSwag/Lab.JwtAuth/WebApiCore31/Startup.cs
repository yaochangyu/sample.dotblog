using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;

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

            // global cors policy
            app.UseCors(x => x
                             .AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            // Add OpenAPI/Swagger middlewares
            app.UseOpenApi();    // Serves the registered OpenAPI/Swagger documents by default on `/swagger/{documentName}/swagger.json`
            app.UseSwaggerUi3(); // Serves the Swagger UI 3 web ui to view the OpenAPI/Swagger documents by default on `/swagger`
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();
            services.AddSingleton<IJwtAuthenticationProvider, JwtAuthenticationProvider>();

            // configure strongly typed settings objects
            var appSettingsSection = this.Configuration.GetSection("AppSettings");
            var appSettings        = appSettingsSection.Get<AppSettings>();

            //services.Configure<AppSettings>(appSettingsSection);
            services.AddSingleton(appSettings);


            // configure jwt authentication
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
                                       {
                                           x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                           x.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                                       })
                    .AddJwtBearer(x =>
                                  {
                                      x.RequireHttpsMetadata = false;
                                      x.SaveToken            = true;
                                      x.TokenValidationParameters = new TokenValidationParameters
                                      {
                                          ValidateIssuerSigningKey = true,
                                          IssuerSigningKey         = new SymmetricSecurityKey(key),
                                          ValidateIssuer           = false,
                                          ValidateAudience         = false
                                      };
                                  });

            services.AddOpenApiDocument(document =>
                                        {
                                            var openApiSecurityScheme = new OpenApiSecurityScheme
                                            {
                                                Type        = OpenApiSecuritySchemeType.ApiKey,
                                                Name        = "Authorization",
                                                In          = OpenApiSecurityApiKeyLocation.Header,
                                                Description = "Type into the textbox: Bearer {your JWT token}."
                                            };
                                            document.AddSecurity("JWT",
                                                                 Enumerable.Empty<string>(),
                                                                 openApiSecurityScheme);

                                            document.OperationProcessors
                                                    .Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
                                        });
        }
    }
}