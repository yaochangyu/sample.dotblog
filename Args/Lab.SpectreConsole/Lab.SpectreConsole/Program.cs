// See https://aka.ms/new-console-template for more information

using Lab.SpectreConsole;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Templates;
using Spectre.Console.Extensions.Hosting;

// var formatter = new CompactJsonFormatter();
var formatter = new ExpressionTemplate(
    "{ {_t: @t, _msg: @m, _props: @p} }\n");
Log.Logger = new LoggerConfiguration()
    // .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatter)
    .WriteTo.File(formatter, "logs/host-.txt", rollingInterval: RollingInterval.Hour)
    .CreateBootstrapLogger();

var currentDomain = AppDomain.CurrentDomain;
currentDomain.UnhandledException += (_, eventArgs) =>
{
    var e = (Exception)eventArgs.ExceptionObject;
    Log.Error(e, "執行命令時發生非預期的錯誤");
};

try
{
    Log.Information("程序開始");
    await Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseConsoleLifetime()
            .UseSpectreConsole(config => { config.AddCommand<FileSizeAsyncCommand>("filesize"); })
            .RunConsoleAsync()
        ;
    Console.WriteLine("程序結束");
    return Environment.ExitCode;
}
catch (Exception e)
{
    Log.Error(e, "執行命令時發生非預期的錯誤");
    return -1;
}