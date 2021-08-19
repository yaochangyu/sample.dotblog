using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc5Net48
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddControllersAsServices(this IServiceCollection services,
                                                                  IEnumerable<Type>       controllerTypes)
        {
            var filter = controllerTypes.Where(t => !t.IsAbstract 
                                                    && !t.IsGenericTypeDefinition)
                                        .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                                                    || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase));

            foreach (var type in filter)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}