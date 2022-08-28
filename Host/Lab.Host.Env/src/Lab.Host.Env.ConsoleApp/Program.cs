// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            var environmentName = hostContext.HostingEnvironment.EnvironmentName;
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
        })
    ;
var host = hostBuilder.Build();
var environment = host.Services.GetService<IHostEnvironment>();
Console.WriteLine($"Environment: {environment.EnvironmentName}");

var configuration = host.Services.GetService<IConfiguration>();
var version = configuration.GetSection("Extension:Version").Value;
Console.WriteLine($"Extension.Version: {version}");

await host.StartAsync();
await host.StopAsync();
Console.WriteLine("Hello, World!");