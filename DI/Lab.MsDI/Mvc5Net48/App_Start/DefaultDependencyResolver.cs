using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc5Net48
{
    /// <summary>
    ///     Provides the default dependency resolver for the application - based on IDependencyResolver, which hhas just two
    ///     methods.
    ///     This is combined dependency resolver for MVC and WebAPI usage.
    /// </summary>
    public class DefaultDependencyResolver : System.Web.Mvc.IDependencyResolver,
                                             System.Web.Http.Dependencies.IDependencyResolver
    {
        protected IServiceScope    scope;
        protected IServiceProvider serviceProvider;

        public DefaultDependencyResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public DefaultDependencyResolver(IServiceScope scope)
        {
            this.scope           = scope;
            this.serviceProvider = scope.ServiceProvider;
        }

        public System.Web.Http.Dependencies.IDependencyScope BeginScope()
        {
            return new DefaultDependencyResolver(this.serviceProvider.CreateScope());
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        public object GetService(Type serviceType)
        {
            return this.serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.serviceProvider.GetServices(serviceType);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.scope?.Dispose();
        }
    }
}