using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
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
            var serviceProvider = DependencyInjectionConfig.Register() as ServiceProvider;

            using (serviceProvider)
            {
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
        }
    }
}