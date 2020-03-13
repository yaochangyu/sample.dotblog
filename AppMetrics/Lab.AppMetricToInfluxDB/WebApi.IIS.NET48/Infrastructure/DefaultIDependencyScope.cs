using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.IIS.NET48.Infrastructure
{
    public class DefaultIDependencyScope : IDependencyScope
    {
        private readonly IServiceScope _serviceScope;

        public DefaultIDependencyScope(IServiceScope serviceScope)
        {
            this._serviceScope = serviceScope;
        }

        public void Dispose()
        {
            this._serviceScope.Dispose();
        }

        public object GetService(Type serviceType)
        {
            return this._serviceScope.ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this._serviceScope.ServiceProvider.GetServices(serviceType);
        }
    }
}