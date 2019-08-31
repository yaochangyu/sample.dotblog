using System.Configuration;
using System.Reflection;
using Topshelf;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Topshelf.Configure();
        }
    }

    class Topshelf
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
    }
}