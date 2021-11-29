using Lab.GracefulShutdown.Net6;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Loader;

var tcs = new TaskCompletionSource();
var sigintReceived = false;

Console.WriteLine("等待以下訊號 SIGINT/SIGTERM");

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("已接收 SIGINT (Ctrl+C)");
    tcs.SetResult();
    sigintReceived = true;
};

AssemblyLoadContext.Default.Unloading += ctx =>
{
    if (!sigintReceived)
    {
        Console.WriteLine("已接收 SIGTERM");
        tcs.SetResult();
    }
    else
    {
        Console.WriteLine("@AssemblyLoadContext.Default.Unloading，已處理 SIGINT，忽略 SIGTERM");
    }
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    if (!sigintReceived)
    {
        Console.WriteLine("已接收 SIGTERM");
        tcs.SetResult();
    }
    else
    {
        Console.WriteLine("@AppDomain.CurrentDomain.ProcessExit，已處理 SIGINT，忽略 SIGTERM");
    }
};

await Host.CreateDefaultBuilder(args)
          .ConfigureServices((hostContext, services) =>
          {
              // services.AddHostedService<GracefulShutdownService_Fail>();
              services.AddHostedService<GracefulShutdownService>();
          })
          .RunConsoleAsync();
Console.WriteLine("下次再來唷~");

