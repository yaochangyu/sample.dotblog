using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Args;
using NLog;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        private static readonly ILogger s_logger;
        private static readonly object  s_lock = new object();
        private static          bool    s_isStart;
        private static          Process s_process;

        static Program()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            GlobalErrorHandler();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (AppManager.IsRunningAsAdministrator())
            {
                if (AppManager.IsAdminAlreadyRunning())
                {
                    Environment.Exit(1);
                    return;
                }

                var arg = Configuration.Configure<CommandArgs>().CreateAndBind(args);

                //TODO:執行緒堵塞
                Application.Run(new Form1(arg));
            }
            else
            {
                if (AppManager.IsClickOnceAlreadyRunning())
                {
                    Environment.Exit(1);
                    return;
                }

                var title = AppManager.GetTitle();

                s_process = AppManager.CreateAdminProcess($"/Title {title}");
                AppManager.CheckAndUpdate();

                //TODO:另一條執行緒檢查更新
                StartCheckUpdateAsync(10000);

                //TODO:執行管理員身分，主執行緒堵塞
                AppManager.RunAsAdminAndWaitForExit(s_process);
            }
        }

        private static void CheckUpdateWithEvent(int checkInterval)
        {
            while (s_isStart)
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                {
                    return;
                }

                try
                {
                    var hasUpdate = ApplicationDeployment.CurrentDeployment.CheckForUpdate();

                    if (hasUpdate)
                    {
                        var updated = ApplicationDeployment.CurrentDeployment.Update();
                        s_isStart = false;
                        s_process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex);
                }

                //catch (DeploymentDownloadException ex)
                //{

                //}
                //catch (InvalidDeploymentException ex)
                //{
                //}
                //catch (InvalidOperationException ex)
                //{
                //}
                finally
                {
                    SpinWait.SpinUntil(() => !s_isStart, checkInterval);
                }
            }
        }

        private static void GlobalErrorHandler()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException                += Unhandled_Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += Unhandled_CurrentDomain_UnhandledException;
        }

        private static void StartCheckUpdateAsync(int checkInterval)
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                return;
            }

            lock (s_lock)
            {
                if (s_isStart)
                {
                    return;
                }

                s_isStart = true;
                Task.Factory.StartNew(() => { CheckUpdateWithEvent(checkInterval); });
            }
        }

        private static void Unhandled_Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var exception = e.Exception;
            s_logger.Error(exception, "Application ThreadException");

            var msg = $"Application ThreadException:{exception}";
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void Unhandled_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            s_logger.Error(exception, "CurrentDomain UnhandledException");
            var msg = $"CurrentDomain UnhandledException:{exception}";
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}