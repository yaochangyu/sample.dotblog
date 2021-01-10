using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WinFormsNet48
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = new ServiceCollection();
            DependencyInjectionConfig.Register(services);
            using (var serviceProvider = services.BuildServiceProvider())
            {
                MainForm.ServiceProvider = serviceProvider;//只有主要表單能使用 Service Locator
                var form = serviceProvider.GetService(typeof(MainForm)) as Form;
                Application.Run(form);
            }

            //Application.Run(new Form1());
        }
    }
}