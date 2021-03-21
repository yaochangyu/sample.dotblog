using System;
using System.Net;
using System.Windows.Forms;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Client.NET5
{
    public class DependencyInjectionConfig
    {
        private static readonly string BaseAddress = "https://localhost:44319/";

        public static void Register(IServiceCollection services)
        {
            ConfigureServices(services);
        }

        private static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            
            services.AddHttpClient<FlurlClient>(client =>
                                                {
                                                    client.BaseAddress = new Uri(BaseAddress);
                                                });
            services.AddSingleton<Form1>();
            return services;
        }
    }
}