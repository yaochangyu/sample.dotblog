using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc5Net48
{
    /// <summary>
    /// Scope 的生命週期錯誤
    /// </summary>
    public class DefaultDependencyResolver2 : IDependencyResolver
    {
        private readonly ServiceProvider _serviceProvider;

        public DefaultDependencyResolver2(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider as ServiceProvider;
        }

        public object GetService(Type serviceType)
        {
            return this._serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this._serviceProvider.GetServices(serviceType);
        }

        public void Dispose()
        {
            this._serviceProvider?.Dispose();
        }
    }
}