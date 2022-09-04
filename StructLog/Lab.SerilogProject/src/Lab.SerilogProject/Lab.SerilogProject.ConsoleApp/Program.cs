using Lab.SerilogProject.ConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
        .CreateBootstrapLogger()
    ;

try
{
    Log.Information("Starting host");

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<LabBackgroundService>();
        })
        .UseSerilog((context, services, config) =>
        {
            var formatter = new JsonFormatter();

            config.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatter)
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File(formatter, "logs/app-.txt", rollingInterval: RollingInterval.Minute);
        });
    ;
    var host = builder.Build();
    host.StartAsync();
    host.StopAsync();
    Console.WriteLine("Bye~~~");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}