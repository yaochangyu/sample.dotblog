using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lab.DynamicAccessor.Accessor
{
    public class DyanmiceAccessManager
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyAccessInfo>> s_properties;

        static DyanmiceAccessManager()
        {
            s_properties = new ConcurrentDictionary<Type, Dictionary<string, PropertyAccessInfo>>();
        }

        public static Dictionary<string, PropertyAccessInfo> GetProperties<T>() where T : class, new()
        {
            var type = typeof(T);

            if (s_properties.TryGetValue(type, out var results) == false)
            {
                var infos = new Dictionary<string, PropertyAccessInfo>();
                foreach (var propertyInfo in type.GetProperties())
                {
                    var info = new PropertyAccessInfo
                    {
                        PropertyInfo = propertyInfo,
                        PropertyName = propertyInfo.Name,
                        Getter       = PropertyAccessor.GetOrCreateGetter(type, propertyInfo),
                        Setter       = PropertyAccessor.GetOrCreateSetter(type, propertyInfo)
                    };
                    infos.Add(propertyInfo.Name, info);
                }

                s_properties.TryAdd(type, infos);
                results = infos;
            }

            return results;
        }
    }
}