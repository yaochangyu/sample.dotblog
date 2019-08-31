using System;
using System.Collections.Generic;

namespace ConsoleApp1.Services
{
    public class ServiceContainer
    {
        internal readonly Dictionary<Type, IService> _serviceCaches;

        public ServiceContainer()
        {
            if (this._serviceCaches == null)
            {
                this._serviceCaches = new Dictionary<Type, IService>();
            }
        }

        public void Add<T>() where T : IService
        {
            var key     = typeof(T);
            var isExist = this._serviceCaches.TryGetValue(key, out var result);
            var service = Activator.CreateInstance(key) as IService;
            if (!isExist)
            {
                this._serviceCaches.Add(key, service);
            }
        }
    }
}