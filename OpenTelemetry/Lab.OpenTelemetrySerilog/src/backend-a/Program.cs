using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_ENDPOINT") ?? "http://localhost:4317";
var backendBUrl = Environment.GetEnvironmentVariable("BACKEND_B_URL") ?? "http://localhost:5200";

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.WithProperty("Application", "backend-a")
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(seqUrl)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = otelEndpoint;
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
    builder.Services.AddHttpClient();

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("backend-a"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }));

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/weatherforecast", async (HttpClient client) =>
        {
            try
            {
                var response = await client.GetStringAsync($"{backendBUrl}/weatherforecast");
                return Results.Content(response, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem($"Error calling Backend-B: {ex.Message}");
            }
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