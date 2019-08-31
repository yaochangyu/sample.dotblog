using System;
using System.Reflection;
using System.Timers;
using Topshelf;

namespace ConsoleApp1
{
    public class DoThing
    {
        private readonly Timer _timer;

        public DoThing()
        {
            this._timer         =  new Timer(1000) {AutoReset = true};
            this._timer.Elapsed += (sender, eventArgs) => Console.WriteLine($"Now Time:{DateTime.Now}");
        }

        public void Start()
        {
            this._timer.Start();
            Console.WriteLine($"Timer Start");
        }

        public void Stop()
        {
            this._timer.Stop();
            Console.WriteLine($"Timer Stop");
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
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