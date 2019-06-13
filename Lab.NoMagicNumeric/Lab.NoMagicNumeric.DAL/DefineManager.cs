using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Lab.NoMagicNumeric.DAL
{
    public static class DefineManager
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, Status>> _caches =
            new ConcurrentDictionary<Type, Dictionary<string, Status>>();

        private static readonly ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>> _enumCaches =
            new ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>>();

        public static DefineAttribute GetDefine(this Enum source)
        {
            var lookup = GetEnumLookup(source);
            return lookup[source.ToString()];
        }

        public static Dictionary<string, DefineAttribute> GetEnumLookup<T>() where T : Enum
        {
            return GetEnumLookup(typeof(T));
        }

        public static Dictionary<string, DefineAttribute> GetEnumLookup(Enum source)
        {
            return GetEnumLookup(source.GetType());
        }

        public static Dictionary<string, DefineAttribute> GetEnumLookup(Type type)
        {
            Dictionary<string, DefineAttribute> results = null;

            if (_enumCaches.ContainsKey(type))
            {
                results = _enumCaches[type];
            }
            else
            {
                results = new Dictionary<string, DefineAttribute>();
                foreach (var item in Enum.GetValues(type))
                {
                    var result = (DefineAttribute) type.GetMember(item.ToString())[0]
                                                       .GetCustomAttributes(typeof(DefineAttribute), false)[0];

                    result.Name  = item.ToString();
                    result.Value = Convert.ToInt32(item);
                    results.Add(result.Name, result);
                }

                _enumCaches.TryAdd(type, results);
            }

            return results;
        }

        public static DefineAttribute GetEnumLookup<T>(string key)
        {
            return GetEnumLookup(typeof(T))[key];
        }

        public static DefineAttribute GetEnumLookup(Type type, string key)
        {
            return GetEnumLookup(type)[key];
        }

        public static Dictionary<string, Status> GetLookup<T>()
        {
            Dictionary<string, Status> result = null;
            var                        type   = typeof(T);

            if (_caches.ContainsKey(type))
            {
                result = _caches[type];
            }
            else
            {
                result = new Dictionary<string, Status>();

                var propertyInfos = type.GetProperties(BindingFlags.Instance |
                                                       BindingFlags.Static   |
                                                       BindingFlags.Public);

                foreach (var property in propertyInfos)
                {
                    var value = property.GetValue(null, null) as Status;
                    result.Add(value.Code, value);
                }

                _caches.TryAdd(type, result);
            }

            return result;
        }
    }
}