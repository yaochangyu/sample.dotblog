using Microsoft.AspNetCore.Mvc;

namespace Lab.Idempotent.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private static readonly List<WeatherForecast> s_repository = new();

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpPost("{temperature}")]
    [Idempotent]
    public WeatherForecast Post(int temperature)
    {
        var data = new WeatherForecast { TemperatureC = temperature, Date = DateTime.UtcNow };
        s_repository.Add(data);

        return data;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        var rng = new Random();
        return s_repository.Select(p => new WeatherForecast
            {
                TemperatureC = p.TemperatureC,
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
    }
}
