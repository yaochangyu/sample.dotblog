using System;
using System.Linq;
using System.Reflection;
using Autofac;
using ConsoleApp1.Services;
using Topshelf;
using Topshelf.Autofac;

namespace ConsoleApp1
{

    public class Worker
    {
        public ILog Log { get; set; }

        public void DoSomething(string command)
        {
            Console.WriteLine("JOB:" + command);
            this.Log.Log(command);
        }
    }

    public interface ILog
    {
        void Log(string msg);
    }

    public class Logger : ILog
    {
        public void Log(string msg)
        {
            Console.WriteLine("LOG:" + msg);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            WindowsServiceConfig.ConfigureWithAutoFact2();
        }

        private static void test3()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Logger>().As<ILog>();

            //透過PropertyAutowired()交由Autofac自動解析
            builder.RegisterType<Worker>().PropertiesAutowired();
            var container = builder.Build();

            var worker = container.Resolve<Worker>();
            worker.DoSomething("Wash the dog");
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

        public static void ConfigureWithAutoFact()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Service2>().As<IService>();
            var assembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(assembly)

                   //.Where(t => t.Name.StartsWith("Service"))
                   .As<IService>()
                   .AsImplementedInterfaces()
                   .Keyed<IService>(k => k.Name);
            ;
            var container = builder.Build();
            foreach (var registration in container.ComponentRegistry.Registrations)
            {
                var name = registration.Activator.LimitType.Name;
                if (container.TryResolveNamed(name, typeof(IService), out var result))
                {
                    var service = (IService) result;
                }
            }

            HostFactory.Run(x =>
                            {
                                x.UseAutofacContainer(container);
                                x.Service<IService>(s =>
                                                    {
                                                        //s.ConstructUsing(hostSettings => container.Resolve<Service2>());
                                                        s.ConstructUsingAutofacContainer();
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

        public static void ConfigureWithAutoFact2()
        {
            var builder  = new ContainerBuilder();
            var assembly = Assembly.GetEntryAssembly();

            builder.RegisterAssemblyTypes(assembly)
                   .Where(x => x.GetInterfaces().Contains(typeof(IService))).AsImplementedInterfaces();


            var container = builder.Build();
            var service1  = container.Resolve<Service1>();
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