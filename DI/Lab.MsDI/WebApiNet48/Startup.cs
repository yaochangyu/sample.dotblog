using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace WebApiNet48
{
    public class Startup
    {
        public static void Bootstrapper(HttpConfiguration config)
        {
            var provider = ConfigureServices().BuildServiceProvider();
            var resolver = new DefaultDependencyResolver(provider);

            config.DependencyResolver = resolver;
        }

        private static ServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddControllersAsServices(typeof(Startup)
                .Assembly
                .GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => typeof(IHttpController).IsAssignableFrom(t)
                            || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)));

            services.AddTransient<ITransientMessager, MultiMessager>()
                                                .AddSingleton<ISingleMessager, MultiMessager>()
                                                .AddScoped<IScopeMessager, MultiMessager>();
            return services;
        }
    }
}