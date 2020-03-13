using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.IIS.NET48.Infrastructure
{
    public class DefaultDependencyResolver : IDependencyResolver
    {
        protected IServiceProvider ServiceProvider;

        public DefaultDependencyResolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IDependencyScope BeginScope()
        {
            return new DefaultIDependencyScope(this.ServiceProvider.CreateScope());
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType)
        {
            return this.ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.ServiceProvider.GetServices(serviceType);
        }
    }
}