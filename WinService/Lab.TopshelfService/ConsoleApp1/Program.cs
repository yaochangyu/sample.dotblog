using System.Reflection;
using ConsoleApp1.Services;
using Topshelf;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WindowsServiceConfig.ConfigureWithMultiService();
        }
    }

    internal class WindowsServiceConfig
    {
        public static void Configure()
        {
            HostFactory.Run(x =>
                            {
                                x.Service<DoThing>(s =>
                                                   {
                                                       s.ConstructUsing(name => new DoThing());
                                                       s.WhenStarted(tc => tc.Start());
                                                       s.WhenStopped(tc => tc.Stop());
                                                   });
                                x.RunAsLocalSystem();
                                var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                                x.SetDescription("Sample Topshelf Host");
                                x.SetDisplayName(assemblyName);
                                x.SetServiceName(assemblyName);
                            });
        }

        public static void ConfigureWithMultiService()
        {
            HostFactory.Run(x =>
                            {
                                x.Service<ServiceManager>(s =>
                                                          {
                                                              ServiceManager.Container.Add<Service1>();
                                                              ServiceManager.Container.Add<Service2>();

                                                              s.ConstructUsing(name => new ServiceManager());
                                                              s.WhenStarted(tc => tc.Start());
                                                              s.WhenStopped(tc => tc.Stop());
                                                          });
                                x.UseNLog();
                                x.RunAsLocalSystem();
                                var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                                x.SetDescription("Sample Topshelf Host");
                                x.SetDisplayName(assemblyName);
                                x.SetServiceName(assemblyName);
                            });
        }

        public static void ConfigureWithNLog()
        {
            HostFactory.Run(x =>
                            {
                                x.Service<DoThing>(s =>
                                                   {
                                                       s.ConstructUsing(name => new DoThing());
                                                       s.WhenStarted(tc => tc.Start());
                                                       s.WhenStopped(tc => tc.Stop());
                                                   });
                                x.UseNLog();
                                x.RunAsLocalSystem();
                                var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                                x.SetDescription("Sample Topshelf Host");
                                x.SetDisplayName(assemblyName);
                                x.SetServiceName(assemblyName);
                            });
        }
    }
}