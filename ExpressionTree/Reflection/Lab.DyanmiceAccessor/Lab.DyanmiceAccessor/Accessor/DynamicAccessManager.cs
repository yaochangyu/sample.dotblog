using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class DynamicAccessManager
    {
        internal static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyAccessInfo>>
            s_properties;

        static DynamicAccessManager()
        {
            s_properties = new ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyAccessInfo>>();
        }

        public static ConcurrentDictionary<string, PropertyAccessInfo> GetProperties(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var type = propertyInfo.DeclaringType;

            if (s_properties.TryGetValue(type, out var infos) == false)
            {
                infos = new ConcurrentDictionary<string, PropertyAccessInfo>();
                var info = CreatePropertyAccessInfo(type, propertyInfo);
                if (info != null)
                {
                    infos.TryAdd(propertyInfo.Name, info);
                    s_properties.TryAdd(type, infos);
                }
            }
            else
            {
                if (infos.TryGetValue(propertyInfo.Name, out var info) == false)
                {
                    info = CreatePropertyAccessInfo(type, propertyInfo);
                    if (info != null)
                    {
                        infos.TryAdd(propertyInfo.Name, info);
                    }
                }
            }

            return infos;
        }

        public static ConcurrentDictionary<string, PropertyAccessInfo> GetProperties(Type type)
        {
            if (s_properties.TryGetValue(type, out var results) == false)
            {
                var infos = new ConcurrentDictionary<string, PropertyAccessInfo>();
                foreach (var propertyInfo in type.GetProperties())
                {
                    var info = new PropertyAccessInfo
                    {
                        PropertyInfo = propertyInfo,
                        PropertyName = propertyInfo.Name,
                        Getter       = PropertyAccessor.GetOrCreateGetter(type, propertyInfo),
                        Setter       = PropertyAccessor.GetOrCreateSetter(type, propertyInfo)
                    };
                    infos.TryAdd(propertyInfo.Name, info);
                }

                s_properties.TryAdd(type, infos);
                results = infos;
            }

            return results;
        }

        public static ConcurrentDictionary<string, PropertyAccessInfo> GetProperties<T>()
            where T : class, new()
        {
            return GetProperties(typeof(T));
        }

        public static ConcurrentDictionary<string, PropertyAccessInfo> GetProperties(object instance)
        {
            return GetProperties(instance.GetType());
        }

        private static PropertyAccessInfo CreatePropertyAccessInfo(Type type, PropertyInfo propertyInfo)
        {
            var info = new PropertyAccessInfo();
            info.PropertyInfo = propertyInfo;
            info.PropertyName = propertyInfo.Name;
            info.Getter       = PropertyAccessor.GetOrCreateGetter(type, propertyInfo);
            info.Setter       = PropertyAccessor.GetOrCreateSetter(type, propertyInfo);
            return info;
        }
    }
}