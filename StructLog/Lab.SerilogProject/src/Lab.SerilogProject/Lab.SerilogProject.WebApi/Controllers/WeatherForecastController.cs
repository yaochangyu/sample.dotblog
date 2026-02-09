using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace Lab.SerilogProject.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        this._logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        // using var scope = this._logger.BeginScope(new Dictionary<string, object>
        // {
        //     ["UserId"] = "svrooij",
        //     ["OperationType"] = "update",
        // });

        // UserId and OperationType are set for all logging events in these brackets
        this._logger.LogInformation(new EventId(2000, "Trace"), "Start {ControllerName}.{MethodName}...",
            nameof(WeatherForecastController), nameof(this.Get));

        var sensorInput = new { Latitude = 25, Longitude = 134 };
        this._logger.LogInformation("Processing {@SensorInput}", sensorInput);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
    
    [HttpPost]
    public IActionResult Post(WeatherForecast forecast)
    {
        this.HttpContext.Request.Headers.TryGetValue("x-my-head", out var head);
        using (LogContext.PushProperty("Action", "AddWeatherForecastToBackendB"))
        using (LogContext.PushProperty("UserID", "user-456")) // Placeholder
        using (LogContext.PushProperty("ProductID", "prod-789")) // Placeholder
        {
            _logger.LogInformation("Received request to add weather forecast: {@Forecast}", forecast);
            // In a real application, you would save this to a database
            // For now, we'll just log it.
            _logger.LogInformation("Weather forecast added successfully (simulated).");
            return Ok(forecast);
        }
    }
}