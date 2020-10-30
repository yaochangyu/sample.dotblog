using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    internal static class Program
    {
        public static ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = Startup.ConfigureServices();
            using (var serviceProvider = services.BuildServiceProvider())
            {
                ServiceProvider = serviceProvider;
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
        }
    }
}