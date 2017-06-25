using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace BLL.WinService
{
    internal static class Program
    {
        ///// <summary>
        ///// The main entry point for the application.
        ///// </summary>
        //static void Main()
        //{
        //    ServiceBase[] ServicesToRun;
        //    ServicesToRun = new ServiceBase[]
        //    {
        //        new Service1()
        //    };
        //    ServiceBase.Run(ServicesToRun);
        //}

        private static void Main(string[] args)
        {
            // Run Service
            ServiceBase[] services =
            {
                new Service1()
            };

            if (Environment.UserInteractive)
            {
                RunInteractive(services);
            }
            else
            {
                ServiceBase.Run(services);
            }
        }

        public static void RunInteractive(ServiceBase[] services)
        {
            Console.WriteLine();
            Console.WriteLine("Install the services in interactive mode.");
            Console.WriteLine();

            // Get the method to invoke on each service to start it
            var onStartMethod =
                typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);

            // Start services loop
            foreach (var service in services)
            {
                Console.WriteLine("Installing {0} ... ", service.ServiceName);
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.WriteLine("Installed {0} ", service.ServiceName);

                Console.WriteLine();
            }

            // Waiting the end
            Console.WriteLine("Press a key to uninstall all services...");
            Console.ReadKey();
            Console.WriteLine();

            // Get the method to invoke on each service to stop it
            var onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

            // Stop loop
            foreach (var service in services)
            {
                Console.Write("Uninstalling {0} ... ", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Uninstalled {0}", service.ServiceName);
            }

            Console.WriteLine();
            Console.WriteLine("All services are uninstalled.");

            // Waiting a key press to not return to VS directly
            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("=== Press a key to quit ===");
                Console.ReadKey();
            }
        }
    }
}