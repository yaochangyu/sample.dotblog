using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiNet48
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="AutofacServiceProviderFactory"/> to the service collection. ONLY FOR PRE-ASP.NET 3.0 HOSTING. THIS WON'T WORK
        /// FOR ASP.NET CORE 3.0+ OR GENERIC HOSTING.
        /// </summary>
        /// <param name="services">The service collection to add the factory to.</param>
        /// <param name="configurationAction">Action on a <see cref="ContainerBuilder"/> that adds component registrations to the container.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddAutofac(this IServiceCollection services, Action<ContainerBuilder> configurationAction = null)
        {
            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacServiceProviderFactory(configurationAction));
        }
    }

}