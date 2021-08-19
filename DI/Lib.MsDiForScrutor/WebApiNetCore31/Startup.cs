using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApiNetCore31
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void AutoConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.Scan(scan => scan.FromAssemblies(assembly)
                                      .AddClasses(classes => classes.AssignableTo<IMessager>())
                                      .AsImplementedInterfaces()
                                      .WithScopedLifetime()
                         );
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

            //AutoConfigureServices(services);
            this.CustomConfigureServices(services);
        }

        public void CustomConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var filterTypes = from type in assembly.GetTypes()
                              where type.IsAbstract == false
                              where typeof(IMessager).IsAssignableFrom(type)
                              //where type.Name.EndsWith("Messsage")
                              select type;
            
            foreach (var type in filterTypes)
            {
                services.AddTransient( type);
            }
        }
    }
}