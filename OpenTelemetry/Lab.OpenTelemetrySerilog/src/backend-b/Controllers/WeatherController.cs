using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace backend_b.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<WeatherController> _logger;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherController(ILogger<WeatherController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            using (LogContext.PushProperty("Action", "GenerateWeatherForecastForBackendB"))
            using (LogContext.PushProperty("UserID", "user-456")) // Placeholder
            using (LogContext.PushProperty("ProductID", "prod-789")) // Placeholder
            {
                _logger.LogInformation("Generating weather forecast for backend-b.");
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        (
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            Summaries[Random.Shared.Next(Summaries.Length)]
                        ))
                    .ToArray();
                _logger.LogInformation("Weather forecast generated successfully.");
                return forecast;
            }
        }

        [HttpPost]
        public IActionResult Post(WeatherForecast forecast)
        {
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
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}