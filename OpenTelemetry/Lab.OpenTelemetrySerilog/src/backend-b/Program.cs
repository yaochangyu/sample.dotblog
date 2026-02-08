using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var otelGrpcEndpoint = Environment.GetEnvironmentVariable("OTEL_GRPC_ENDPOINT") ?? "http://localhost:4317";
var otelHttpEndpoint = Environment.GetEnvironmentVariable("OTEL_HTTP_ENDPOINT") ?? "http://localhost:4318";

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.WithProperty("Application", "backend-b")
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(seqUrl)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = otelGrpcEndpoint;
            options.Protocol = OtlpProtocol.Grpc;
        })
        .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Day)
        .CreateBootstrapLogger()
    ;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("backend-b"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter("otlp-grpc", options =>
            {
                options.Endpoint = new Uri(otelGrpcEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            })
            .AddOtlpExporter("otlp-http", options =>
            {
                options.Endpoint = new Uri(otelHttpEndpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter("otlp-grpc", options =>
            {
                options.Endpoint = new Uri(otelGrpcEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            })
            .AddOtlpExporter("otlp-http", options =>
            {
                options.Endpoint = new Uri(otelHttpEndpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            }));

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    (
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}