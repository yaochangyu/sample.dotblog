using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleAppNetFx48
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                                   {
                                       services.AddHostedService<Worker>();
                                   });
        
        public static void Main(string[] args)
        {
            // HostFactory.Run(x =>
            //                 {
            //                     x.Service<DoThing>(s =>
            //                                        {
            //                                            s.ConstructUsing(name => new DoThing());
            //                                            s.WhenStarted(tc => tc.Start());
            //                                            s.WhenStopped(tc => tc.Stop());
            //                                        });
            //                     x.UseNLog();
            //                     x.RunAsLocalSystem();
            //                     var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            //                     x.SetDescription("Sample Topshelf Host");
            //                     x.SetDisplayName(assemblyName);
            //                     x.SetServiceName(assemblyName);
            //                 });
            
            var hostBuilder = CreateHostBuilder(args);
            hostBuilder.Build().Run();
        }
    }
}