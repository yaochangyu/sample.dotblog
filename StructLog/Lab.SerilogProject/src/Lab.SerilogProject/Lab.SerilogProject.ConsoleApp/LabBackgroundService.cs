using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab.SerilogProject.ConsoleApp;

public class LabBackgroundService : BackgroundService
{
    private readonly ILogger<LabBackgroundService> _logger;

    public LabBackgroundService(ILogger<LabBackgroundService> logger)
    {
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation(new EventId(2000, "Trace"), "Start {ClassName}.{MethodName}...",
            nameof(LabBackgroundService), nameof(this.ExecuteAsync));

        var sensorInput = new { Latitude = 25, Longitude = 134 };
        this._logger.LogInformation("Processing {@SensorInput}", sensorInput);
    }
}