using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;

namespace WebApiNet48
{
    public class DefaultDependencyResolver : IDependencyResolver
    {
        protected IServiceProvider ServiceProvider { get; set; }

        public DefaultDependencyResolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return this.ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.ServiceProvider.GetServices(serviceType);
        }

        public IDependencyScope BeginScope()
        {
            return new DefaultDependencyResolver(this.ServiceProvider.CreateScope().ServiceProvider);
        }

        public void Dispose()
        {
            // you can implement this interface just when you use .net core 2.0
            // this.ServiceProvider.Dispose();
        }
    }

    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddControllersAsServices(this IServiceCollection services,
            IEnumerable<Type> controllerTypes)
        {
            foreach (var type in controllerTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
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