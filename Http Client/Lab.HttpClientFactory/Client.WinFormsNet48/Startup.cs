using System;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WinFormsNet48
{
    public class Startup
    {
        private static readonly string BaseAddress = "https://localhost:44319/";

        public static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("localhost",
                                   p =>
                                   {
                                       p.BaseAddress = new Uri(BaseAddress);
                                   });

            services.AddHttpClient<LabService>(client =>
                                               {
                                                   client.BaseAddress = new Uri(BaseAddress);
                                               });
            return services.AddSingleton<Form1>();
        }
    }
}