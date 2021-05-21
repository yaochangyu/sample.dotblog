using System;
using System.Collections.Generic;
using System.Reflection;
using NET5.TestProject.File;

namespace NET5.TestProject
{
    public static class ServiceProviderExtension
    {
        public static T GetService<T>(this IServiceProvider provider, string name)
        {
            var pool = (Func<string, IFileProvider>) provider.GetService(typeof(Func<string, IFileProvider>));
            return (T) pool(name);
        }

        public static List<Type> GetTypesAssignableFrom<T>(this Assembly assembly)
        {
            return assembly.GetTypesAssignableFrom(typeof(T));
        }

        public static List<Type> GetTypesAssignableFrom(this Assembly assembly, Type compareType)
        {
            var results = new List<Type>();
            foreach (var type in assembly.DefinedTypes)
            {
                if (compareType.IsAssignableFrom(type)
                    && compareType != type
                )
                {
                    results.Add(type);
                }
            }

            return results;
        }
    }
}