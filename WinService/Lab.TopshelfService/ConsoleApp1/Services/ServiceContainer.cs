using System;
using System.Collections.Generic;

namespace ConsoleApp1.Services
{
    public class ServiceContainer
    {
        internal readonly Dictionary<Type, object> _serviceCaches;

        public ServiceContainer()
        {
            if (this._serviceCaches == null)
            {
                this._serviceCaches = new Dictionary<Type, object>();
            }
        }

        public void Add<T>()
        {
            var key     = typeof(T);
            var isExist = this._serviceCaches.TryGetValue(key, out var result);
            var service = Activator.CreateInstance(key);
            if (!isExist)
            {
                this._serviceCaches.Add(key, service);
            }
        }
    }
}