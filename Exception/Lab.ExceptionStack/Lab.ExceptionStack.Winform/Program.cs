using System;
using System.Threading;
using System.Windows.Forms;
using Lab.ExceptionStack.BLL;
using NLog;

namespace Lab.ExceptionStack.Winform
{
    internal static class Program
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var exception = e.Exception;
            Show(exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
            Show(exception);
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException                += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void Show(Exception exception)
        {
            var errorDescription = exception.GetCurrentErrorDescription();
            var errorMsg         = $"{errorDescription.Description},{exception.Message}";
            s_logger.Error(exception, errorMsg);
            MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}