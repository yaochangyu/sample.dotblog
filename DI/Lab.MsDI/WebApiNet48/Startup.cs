using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiNet48
{
    public class Startup
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static void Bootstrapper(HttpConfiguration config)
        {
            var provider = ConfigureServices().BuildServiceProvider();

            var resolver = new DefaultDependencyResolver(provider);

            //ServiceScopeModule.SetServiceProvider(provider);
            //var resolver1 = new ServiceProviderDependencyResolver(provider);

            config.DependencyResolver = resolver;
            ServiceProvider           = provider;
        }

        private static ServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddControllersAsServices(typeof(Startup)
                                              .Assembly
                                              .GetExportedTypes()
                                              .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                                              .Where(t => typeof(IHttpController).IsAssignableFrom(t)
                                                          || t.Name.EndsWith("Controller",
                                                                             StringComparison.OrdinalIgnoreCase)));

            services.AddTransient<ITransientMessager, MultiMessager>()
                    .AddSingleton<ISingleMessager, MultiMessager>()
                    .AddScoped<IScopeMessager, MultiMessager>();
            return services;
        }
    }
}