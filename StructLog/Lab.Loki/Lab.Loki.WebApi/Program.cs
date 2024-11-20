using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.Grafana.Loki;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Hour)
    .WriteTo.Seq("http://localhost:5341")
    .WriteTo.GrafanaLoki("http://localhost:3100")
    .CreateLogger();
Log.Information("Starting web host");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers()
        ;
    var jsonFormatter = new JsonFormatter();
    builder.Host
        .UseSerilog((context, services, config) =>
                        config.ReadFrom.Configuration(context.Configuration)
                            .ReadFrom.Services(services)
                            .Enrich.FromLogContext()
                            .WriteTo.Console(jsonFormatter) //正式環境不要用 Console，除非有 Log Provider 專門用來收集 Console Log
                            .WriteTo.Seq("http://localhost:5341") //log server
                            .WriteTo.File("logs/aspnet-.txt", rollingInterval: RollingInterval.Minute) //正式環境不要用 File
        );

    // 確定物件都有設定 DI Container
    builder.Host.UseDefaultServiceProvider(p =>
    {
        p.ValidateScopes = true;
        p.ValidateOnBuild = true;
    });
    var configuration = builder.Configuration;

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        // app.UseSwagger();
        // app.UseSwaggerUI(options =>
        //                      options.SwaggerEndpoint("/swagger/v1/swagger.yaml",
        //                                              "Swagger Demo Documentation v1"));
    }
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthorization();
    app.MapDefaultControllerRoute();
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        //注册Web API Controller
        endpoints.MapControllers();

        //注册MVC Controller模板 {controller=Home}/{action=Index}/{id?}
        // endpoints.MapDefaultControllerRoute();

        //注册健康检查
        // endpoints.MapHealthChecks("/_hc");
    });
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.MapControllers();
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

namespace JobBank1111.Job.WebAPI
{
    public partial class Program
    {
    }
}