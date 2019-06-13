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

        private static readonly ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>> _nameCaches =
            new ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>>();

        private static readonly ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>> _valueCaches =
            new ConcurrentDictionary<Type, Dictionary<string, DefineAttribute>>();

        public static DefineAttribute GetDefineByName(this Enum source)
        {
            return GetLookupByName(source.GetType())[source.ToString()];
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

        public static Dictionary<string, DefineAttribute> GetLookupByName<T>() where T : Enum
        {
            return GetLookupByName(typeof(T));
        }

        public static Dictionary<string, DefineAttribute> GetLookupByName(Type type)
        {
            Dictionary<string, DefineAttribute> results = null;

            if (_nameCaches.ContainsKey(type))
            {
                results = _nameCaches[type];
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

                _nameCaches.TryAdd(type, results);
            }

            return results;
        }

        public static DefineAttribute GetLookupByName<T>(string key)
        {
            return GetLookupByName(typeof(T))[key];
        }

        public static DefineAttribute GetLookupByName(Type type, string key)
        {
            return GetLookupByName(type)[key];
        }

        public static DefineAttribute GetLookupByValue<T>(string value)
        {
            return GetLookupByValue(typeof(T))[value];
        }

        public static DefineAttribute GetLookupByValue(Type type, string value)
        {
            return GetLookupByValue(type)[value];
        }

        public static Dictionary<string, DefineAttribute> GetLookupByValue<T>()
        {
            return GetLookupByValue(typeof(T));
        }

        public static Dictionary<string, DefineAttribute> GetLookupByValue(Type type)
        {
            Dictionary<string, DefineAttribute> results = null;

            if (_valueCaches.ContainsKey(type))
            {
                results = _valueCaches[type];
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
                    results.Add(result.Value.ToString(), result);
                }

                _valueCaches.TryAdd(type, results);
            }

            return results;
        }
    }
}