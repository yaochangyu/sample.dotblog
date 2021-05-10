using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiNet48
{
    public class DefaultDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;
        private          IServiceScope    _serviceScope;

        public DefaultDependencyResolver(IServiceProvider serviceProvider, IServiceScope serviceScope = null)
        {
            this._serviceProvider = serviceProvider;
            this._serviceScope    = serviceScope;
        }

        public object GetService(Type serviceType)
        {
            return this._serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this._serviceProvider.GetServices(serviceType);
        }

        public IDependencyScope BeginScope()
        {
            this._serviceScope = this._serviceProvider.CreateScope();
            return new DefaultDependencyResolver(this._serviceScope.ServiceProvider,this._serviceScope);
        }

        public void Dispose()
        {
            this._serviceScope?.Dispose();
        }
    }
}