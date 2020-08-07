using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public static class DataTableExtensions
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> s_propertyInfos;

        static DataTableExtensions()
        {
            if (s_propertyInfos == null)
            {
                s_propertyInfos = new ConcurrentDictionary<Type, List<PropertyInfo>>();
            }
        }

        public static List<T> ToList<T>(this DataTable source) where T : class, new()
        {
            var targets    = new List<T>();
            var columns    = source.Columns;
            var targetType = typeof(T);

            var propertyInfos = GetPropertyInfos(targetType);

            foreach (var row in source.AsEnumerable())
            {
                var targetInstance = new T();

                //var targetInstance     = Activator.CreateInstance<T>();
                foreach (var propertyInfo in propertyInfos)
                {
                    var propertyName = propertyInfo.Name;
                    var hasColumn    = columns.Contains(propertyName);
                    if (hasColumn == false)
                    {
                        continue;
                    }

                    var sourceValue = row[propertyName];
                    var accessor = DynamicMemberManager.Property.GetOrCreate(propertyInfo);
                    accessor.SetValue(targetInstance, sourceValue);
                }

                targets.Add(targetInstance);
            }

            return targets;
        }

        private static List<PropertyInfo> GetPropertyInfos(Type type)
        {
            if (s_propertyInfos.TryGetValue(type, out var propertyInfos) == false)
            {
                propertyInfos = new List<PropertyInfo>();
                foreach (var propertyInfo in type.GetProperties())
                {
                    DynamicMemberManager.Property.GetOrCreate(propertyInfo);
                    propertyInfos.Add(propertyInfo);
                }

                s_propertyInfos.TryAdd(type, propertyInfos);
            }

            return propertyInfos;
        }
    }
}