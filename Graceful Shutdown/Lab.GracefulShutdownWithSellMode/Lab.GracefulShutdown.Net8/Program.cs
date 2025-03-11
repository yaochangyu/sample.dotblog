using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Loader;
using Lab.GracefulShutdown.Net8;
using Serilog;
using Serilog.Formatting.Json;

var sigintReceived = false;

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
        .CreateBootstrapLogger()
    ;
Log.Information($"Process id: {Process.GetCurrentProcess().Id}");
Log.Information("等待以下訊號 SIGINT/SIGTERM");

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Log.Information("已接收 SIGINT (Ctrl+C)");
    sigintReceived = true;
};

AssemblyLoadContext.Default.Unloading += ctx =>
{
    if (!sigintReceived)
    {
        Log.Information("已接收 SIGTERM，AssemblyLoadContext.Default.Unloading");
    }
    else
    {
        Log.Information("@AssemblyLoadContext.Default.Unloading，已處理 SIGINT，忽略 SIGTERM");
    }
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    if (!sigintReceived)
    {
        Log.Information("已接收 SIGTERM，ProcessExit");
    }
    else
    {
        Log.Information("@AppDomain.CurrentDomain.ProcessExit，已處理 SIGINT，忽略 SIGTERM");
    }
};

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));
        // services.AddHostedService<GracefulShutdownService>();
        services.AddHostedService<GracefulShutdownService1>();
        // services.AddHostedService<GracefulShutdownService_Fail>();
    })
    .UseSerilog((context, services, config) =>
    {
        var formatter = new JsonFormatter();
        config.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatter)
            .WriteTo.File(formatter, "logs/app-.txt", rollingInterval: RollingInterval.Minute);
    })
    .RunConsoleAsync();

Log.Information("下次再來唷~");