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
        // .MinimumLevel.Information()
        // .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        // .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
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
    builder.Services.AddControllers(); 

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

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi(); 
    }

    app.MapControllers();
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