using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors.Security;
using WebApiCore31.Security;

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
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            // Add OpenAPI/Swagger middlewares
            app.UseOpenApi();    // Serves the registered OpenAPI/Swagger documents by default on `/swagger/{documentName}/swagger.json`
            app.UseSwaggerUi3(); // Serves the Swagger UI 3 web ui to view the OpenAPI/Swagger documents by default on `/swagger`
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();

            // configure basic authentication
            services.AddAuthentication("Basic")
                    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

            // configure DI for application services
            //services.AddScoped<IBasicAuthenticationProvider, BasicAuthenticationProvider>();
            services.AddTransient<IBasicAuthenticationProvider, BasicAuthenticationProvider>();

            // Add OpenAPI v3 document
            //services.AddOpenApiDocument();

            services.AddOpenApiDocument(config =>
                                        {
                                            var apiScheme = new OpenApiSecurityScheme
                                            {
                                                Type = OpenApiSecuritySchemeType.Basic,
                                                Name = "Authorization",
                                                In   = OpenApiSecurityApiKeyLocation.Header

                                                //Description = "Basic U3dhZ2dlcjpUZXN0"
                                            };

                                            config.AddSecurity("Basic", Enumerable.Empty<string>(),
                                                               apiScheme);

                                            config.OperationProcessors
                                                  .Add(new AspNetCoreOperationSecurityScopeProcessor("Basic"));
                                        });

            // Add Swagger v2 document
            // services.AddSwaggerDocument();
        }
    }
}