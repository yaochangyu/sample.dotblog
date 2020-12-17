using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WinFormsNet48
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = Startup.ConfigureServices();
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
            //Application.Run(new Form1());
        }
    }
}
