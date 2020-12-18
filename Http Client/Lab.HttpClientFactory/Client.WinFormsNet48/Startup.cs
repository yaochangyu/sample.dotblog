using System;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WinFormsNet48
{
    public class Startup
    {
        private static readonly string BaseAddress = "https://localhost:44319/";

        public static IServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            //注入 HttpClientFactory
            services.AddHttpClient("lab",
                                   p => { p.BaseAddress = new Uri(BaseAddress); });
            services.AddSingleton<LabService2>();
            services.AddSingleton<Form2>();

            // LabService 注入 HttpClient
            services.AddHttpClient<LabService>(client =>
                                               {
                                                   client.BaseAddress = new Uri(BaseAddress);
                                               });
            services.AddSingleton<Form1>();
            services.AddSingleton<MainForm>();
            return services;
        }
    }
}