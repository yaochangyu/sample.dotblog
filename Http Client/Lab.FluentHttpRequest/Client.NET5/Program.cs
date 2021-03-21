using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows.Forms;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Client.NET5
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = new ServiceCollection();
            DependencyInjectionConfig.Register(services);
            // FlurlHttp.Configure(settings => settings.ConnectionLeaseTimeout = TimeSpan.FromMinutes(2));
            FlurlHttp.Configure(settings => settings.HttpClientFactory = new ConnectionLifetimeHttpClientFactory());
            using (var provider = services.BuildServiceProvider())
            {
                var client = provider.GetService<HttpClient>();

                var mainForm = provider.GetService<Form1>();
                Application.Run(mainForm);
            }
        }
    }
}