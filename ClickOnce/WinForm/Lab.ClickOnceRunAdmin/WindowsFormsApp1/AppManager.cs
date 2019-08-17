using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace WindowsFormsApp1
{
    internal static class AppManager
    {
        private static readonly string s_extendName = ".appref-ms";
        private static          Mutex  s_mutexAdmin;
        private static          Mutex  s_mutexClickOnce;

        public static void CheckAndUpdate()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                return;
            }

            if (ApplicationDeployment.CurrentDeployment.CheckForUpdate())
            {
                ApplicationDeployment.CurrentDeployment.Update();
            }
        }

        public static Process CreateAdminProcess(string args = "")
        {
            var processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase);
            var process          = new Process();

            processStartInfo.UseShellExecute = true;
            processStartInfo.Verb            = "runas";
            processStartInfo.Arguments       = args;
            processStartInfo.CreateNoWindow  = true;
            process.StartInfo                = processStartInfo;
            return process;
        }

        public static AppDeployInfo GetCurrentInfo()
        {
            var result = new AppDeployInfo();

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                var deployment = ApplicationDeployment.CurrentDeployment;

                var manifest = GetManifest();
                try
                {
                    var uri = deployment.UpdateLocation;
                    result = GetRemoteInfo(uri);
                }
                catch (Exception e)
                {
                    var activationUri = deployment.ActivationUri;
                    result.ProductName                = manifest.Product;
                    result.PublisherName              = manifest.Publisher;
                    result.CurrentVersion             = deployment.CurrentVersion;
                    result.HasUpdateVersion           = result.UpdateVersion > result.CurrentVersion;
                    result.UpdatedApplicationFullName = deployment.UpdatedApplicationFullName;
                    result.UpdateLocation             = deployment.UpdateLocation;
                    result.UpdateVersion              = deployment.UpdatedVersion;
                    result.Arguments                  = GetArgs();
                    result.DesktopShortcutPath        = GetDesktopShortcutPath(manifest.Product);
                    result.StartMenuShortcutPath =
                        GetStartMenuShortcutPath(manifest.Publisher, manifest.Product);

                    result.ActivationLocation =
                        activationUri == null ? Application.ExecutablePath : activationUri.ToString();
                }
            }
            else
            {
                var assembly        = Assembly.GetEntryAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                result.ProductName        = fileVersionInfo.ProductName;
                result.CurrentVersion     = new Version(fileVersionInfo.ProductVersion);
                result.ActivationLocation = assembly.Location;
            }

            return result;
        }

        public static string GetTitle()
        {
            var appInfo = GetCurrentInfo();
            var title   = $"{appInfo.ProductName} ver.{appInfo.CurrentVersion}";
            return title;
        }

        public static bool IsAdminAlreadyRunning()
        {
            var exeName      = GetExecuteName();
            var canCreateNew = false;

            try
            {
                s_mutexAdmin = new Mutex(true, $"Global\\{exeName}.Admin", out canCreateNew);
            }
            catch (Exception)
            {
            }

            if (canCreateNew)
            {
                s_mutexAdmin.ReleaseMutex();
            }
            else
            {
                var msg = "Only one instance at a time.";
                MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(1);
            }

            return !canCreateNew;
        }

        public static bool IsClickOnceAlreadyRunning()
        {
            var exeName      = GetExecuteName();
            var canCreateNew = false;

            try
            {
                s_mutexClickOnce = new Mutex(true, $"Global\\{exeName}.ClickOnce", out canCreateNew);
            }
            catch (Exception)
            {
            }

            if (canCreateNew)
            {
                s_mutexClickOnce.ReleaseMutex();
            }
            else
            {
                var msg = "Only one instance at a time.";
                MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Environment.Exit(1);
            }

            return !canCreateNew;
        }

        public static bool IsRunningAsAdministrator()
        {
            // Get current Windows user
            var windowsIdentity = WindowsIdentity.GetCurrent();

            // Get current Windows user principal
            var windowsPrincipal = new WindowsPrincipal(windowsIdentity);

            // Return TRUE if user is in role "Administrator"
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RunAsAdminAndWaitForExit(Process process = null)
        {
            if (IsRunningAsAdministrator())
            {
                return;
            }

            try
            {
                if (process == null)
                {
                    process = CreateAdminProcess();
                }

                process.Start();
                process.WaitForExit();
                Environment.Exit(1);
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) //The operation was canceled by the user.
                {
                    MessageBox.Show("Why did you not selected Yes?\r\nLet me one more time",
                                    "Info",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Stop);
                }
                else
                {
                    throw new Exception("Something went wrong :-(");
                }
            }
        }

        private static string GetArgs()
        {
            var args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;

            if (args                          != null
                && args.ActivationData        != null
                && args.ActivationData.Length > 0)
            {
                var url = new Uri(args.ActivationData[0], UriKind.Absolute);
                return url.Query;
            }

            return null;
        }

        private static string GetDesktopShortcutPath(string productName)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return $"{desktopPath}\\{productName}{s_extendName}";
        }

        private static string GetExecuteName()
        {
            var dllPath  = Assembly.GetExecutingAssembly().Location;
            var fileInfo = new FileInfo(dllPath);
            var exeName  = fileInfo.Name;
            return exeName;
        }

        private static DeployManifest GetManifest()
        {
            if (AppDomain.CurrentDomain.ActivationContext == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(AppDomain.CurrentDomain
                                                          .ActivationContext
                                                          .DeploymentManifestBytes))
            {
                return (DeployManifest) ManifestReader.ReadManifest("Deployment", stream, true);
            }
        }

        private static AppDeployInfo GetRemoteInfo(Uri uri)
        {
            var url           = uri.ToString();
            var xmlReader     = XmlReader.Create(url);
            var xmlSerializer = new XmlSerializer(typeof(assembly));
            var assembly      = (assembly) xmlSerializer.Deserialize(xmlReader);
            var result        = Migration(assembly);
            return result;
        }

        private static string GetStartMenuShortcutPath(string publisherName, string productName)
        {
            var startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            return $"{startMenuPath}\\Programs\\{publisherName}\\{productName}{s_extendName}";
        }

        private static AppDeployInfo Migration(assembly source)
        {
            var desktopPath   = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var result = new AppDeployInfo
            {
                ProductName   = source.description.product,
                PublisherName = source.description.publisher
            };
            result.DesktopShortcutPath = $"{desktopPath}\\{source.description.product}{s_extendName}";
            result.StartMenuShortcutPath =
                $"{startMenuPath}\\Programs\\{result.PublisherName}\\{result.ProductName}{s_extendName}";

            result.UpdateVersion = new Version(source.assemblyIdentity.version);
            result.Name          = source.assemblyIdentity.name;
            result.Language      = source.assemblyIdentity.language;
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                var deployment    = ApplicationDeployment.CurrentDeployment;
                var activationUri = deployment.ActivationUri;

                result.ActivationLocation =
                    activationUri == null ? Application.ExecutablePath : activationUri.ToString();
                result.CurrentVersion             = deployment.CurrentVersion;
                result.HasUpdateVersion           = result.UpdateVersion > result.CurrentVersion;
                result.UpdatedApplicationFullName = deployment.UpdatedApplicationFullName;
                result.UpdateLocation             = deployment.UpdateLocation;
                result.Arguments                  = GetArgs();
            }
            else
            {
                result.ActivationLocation         = Application.ExecutablePath;
                result.CurrentVersion             = new Version(Application.ProductVersion);
                result.HasUpdateVersion           = false;
                result.UpdatedApplicationFullName = null;
                result.UpdateLocation             = null;
            }

            return result;
        }
    }
}