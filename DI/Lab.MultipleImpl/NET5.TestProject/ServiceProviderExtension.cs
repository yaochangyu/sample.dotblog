using System;
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
    }
}