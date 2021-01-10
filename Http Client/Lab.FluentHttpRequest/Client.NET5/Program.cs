using System;
using System.Windows.Forms;
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
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = new ServiceCollection();
            DependencyInjectionConfig.Register(services);
            using (var provider = services.BuildServiceProvider())
            {
                var mainForm = provider.GetService<Form1>();
                Application.Run(mainForm);
            }
        }
    }
}