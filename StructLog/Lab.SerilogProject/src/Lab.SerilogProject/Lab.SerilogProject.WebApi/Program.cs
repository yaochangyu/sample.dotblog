using Lab.SerilogProject.WebApi;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        // .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(seqUrl)
        .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
        .CreateBootstrapLogger()
    ;
try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // builder.Host.UseSerilog(); //<=== 讓 Host 使用 Serilog 
    var formatter = new JsonFormatter();
    // var formatter = new MessageTemplateTextFormatter();
    // var formatter = new RawFormatter();
    // var formatter = new RenderedCompactJsonFormatter();
    // var formatter = new CompactJsonFormatter();
    // var formatter = new ExpressionTemplate(
    //     "{ {_t: @t, _msg: @m, _props: @p} }\n");
    builder.Host.UseSerilog((context, services, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatter)
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.File(formatter, "logs/aspnet-.txt", rollingInterval: RollingInterval.Minute);
    });

    // Add services to the container.

    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<TraceMiddleware>();
    app.UseHttpsRedirection();
    // app.UseSerilogRequestLogging(); //<=== 每一個 Request 使用 Serilog 記錄下來 
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId", "svrooij");
            diagnosticContext.Set("OperationType", "update");
        };
    }); 
    app.UseAuthorization();

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