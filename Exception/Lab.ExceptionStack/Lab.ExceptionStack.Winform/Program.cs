using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Lab.ExceptionStack.BLL;

namespace Lab.ExceptionStack.Winform
{
    internal static class Program
    {
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Error(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
            Error(exception);
        }

        private static string Error(Exception ex)
        {
            var stack   = new StackTrace(ex, true);
            var frames  = stack.GetFrames();
            var builder = new StringBuilder();
            foreach (var frame in frames)
            {
                var methodBase = frame.GetMethod();
                var errorDescription = methodBase.GetCustomAttributes(typeof(ErrorDescription), true)
                                           .Select(p => ((ErrorDescription) p).Description)
                                           .FirstOrDefault();
                builder.Append($"Method:{methodBase.Name},{errorDescription}");
            }

            return builder.ToString();

            //自己來
            //var stack = ex.ToString();
            //s_logger.Error(stack); //NLog 4.0

            //交給NLog,NLog.Config必須要設定
            //s_logger.Error(ex, "非預期例外捕捉"); //NLog 4.0
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
    }
}