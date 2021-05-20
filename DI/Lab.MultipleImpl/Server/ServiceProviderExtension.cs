using System;

namespace Server
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