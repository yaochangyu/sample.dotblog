using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetApp48.Behaviors;
using PowerArgs;

namespace NetApp48
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        internal static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //綁定多個行為
            Args.InvokeAction<MultipleBehavior>(args);
            Application.Run(new Form1());
        }
    }
}
