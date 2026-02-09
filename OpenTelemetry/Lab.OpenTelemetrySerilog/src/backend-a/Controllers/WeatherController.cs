using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace backend_a.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<WeatherController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _backendBUrl;

        public WeatherController(ILogger<WeatherController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _backendBUrl = Environment.GetEnvironmentVariable("BACKEND_B_URL") ?? "http://localhost:5200";
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (LogContext.PushProperty("Action", "GetWeatherForecastFromBackendA"))
            using (LogContext.PushProperty("UserID", "user-123")) // Placeholder
            using (LogContext.PushProperty("ProductID", "prod-456")) // Placeholder
            {
                _logger.LogInformation("Calling Backend-B weatherforecast endpoint at {BackendBUrl}", _backendBUrl);
                try
                {
                    var response = await _httpClient.GetStringAsync($"{_backendBUrl}/weatherforecast");
                    _logger.LogInformation("Successfully received response from Backend-B.");
                    return Content(response, "application/json");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error calling Backend-B: {ErrorMessage}", ex.Message);
                    return Problem($"Error calling Backend-B: {ex.Message}");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(WeatherForecast forecast)
        {
            using (LogContext.PushProperty("Action", "AddWeatherForecastViaBackendA"))
            using (LogContext.PushProperty("UserID", "user-123")) // Placeholder
            using (LogContext.PushProperty("ProductID", "prod-456")) // Placeholder
            {
                _logger.LogInformation("Received request to add weather forecast: {@Forecast}", forecast);
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{_backendBUrl}/weatherforecast", forecast);
                    response.EnsureSuccessStatusCode(); // Throws if not a success code.
                    _logger.LogInformation("Successfully forwarded and added weather forecast to Backend-B.");
                    return Ok(forecast);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error forwarding weather forecast to Backend-B: {ErrorMessage}", ex.Message);
                    return Problem($"Error forwarding weather forecast to Backend-B: {ex.Message}");
                }
            }
        }
    }
}

// Assuming WeatherForecast is defined in backend-b and used here for consistency
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}