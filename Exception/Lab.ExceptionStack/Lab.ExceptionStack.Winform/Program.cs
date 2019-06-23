using System;
using System.Threading;
using System.Windows.Forms;
using Lab.ExceptionStack.BLL;

namespace Lab.ExceptionStack.Winform
{
    internal static class Program
    {
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var exception        = e.Exception;
            var errorDescription = exception.GetCurrentErrorDescription();
            var errorMsg         = $"{errorDescription.Description},{exception.Message}";
            Show($"{errorMsg}");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception        = (Exception) e.ExceptionObject;
            var errorDescription = exception.GetCurrentErrorDescription();
            var errorMsg         = $"{errorDescription.Description},{exception.Message}";
            Show($"{errorMsg}");
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add handler to handle the exception raised by main threads
            Application.ThreadException += Application_ThreadException;

            // Add handler to handle the exception raised by additional threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void Show(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}