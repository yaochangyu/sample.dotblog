using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiNet48
{
    internal class ServiceProviderDependencyResolver : IDependencyResolver
    {
        protected IServiceProvider ServiceProvider { get; set; }

        public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            if (HttpContext.Current?.Items[typeof(IServiceScope)] is IServiceScope scope)
            {
                return scope.ServiceProvider.GetService(serviceType);
            }

            return this.ServiceProvider.GetService(serviceType);
            throw new InvalidOperationException("IServiceScope not provided");
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (HttpContext.Current?.Items[typeof(IServiceScope)] is IServiceScope scope)
            {
                return scope.ServiceProvider.GetServices(serviceType);
            }

            return this.ServiceProvider.GetServices(serviceType);
            throw new InvalidOperationException("IServiceScope not provided");
        }

        public IDependencyScope BeginScope()
        {
            return new DefaultDependencyResolver(this.ServiceProvider.CreateScope().ServiceProvider);
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}