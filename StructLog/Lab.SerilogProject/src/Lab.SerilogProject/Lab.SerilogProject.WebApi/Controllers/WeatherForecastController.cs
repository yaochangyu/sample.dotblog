using Microsoft.AspNetCore.Mvc;

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
}