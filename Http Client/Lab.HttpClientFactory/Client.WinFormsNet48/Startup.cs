using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

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

            // LabService 注入 HttpClient
            //services.AddHttpClient<LabService>(client => { client.BaseAddress = new Uri(BaseAddress); });

            services.AddHttpClient<LabService>(client => { client.BaseAddress = new Uri(BaseAddress); })

                    //Polly Retory
                    .AddPolicyHandler(HttpPolicyExtensions

                                      //HandleTransientHttpError 包含 5xx 及 408 錯誤
                                      .HandleTransientHttpError()

                                      //404錯誤
                                      .OrResult(p => p.StatusCode == HttpStatusCode.NotFound)
                                      .WaitAndRetryAsync(6, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
                                     );

            services.AddSingleton<Form2>();
            services.AddSingleton<Form1>();
            services.AddSingleton<MainForm>();
            return services;
        }
    }
}