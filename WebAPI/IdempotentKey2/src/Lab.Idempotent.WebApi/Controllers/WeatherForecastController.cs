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
    public async Task<ActionResult<WeatherForecast>> Post(int temperature, CancellationToken cancel = default)
    {
        // 模擬真實業務處理時間，讓並發測試（concurrent test）能可靠觸發分散式鎖
        await Task.Delay(300, cancel);

        var rng = new Random();
        var data = new WeatherForecast
        {
            TemperatureC = temperature,
            Summary =  Summaries[rng.Next(Summaries.Length)],
            Date = DateTime.UtcNow
        };
        s_repository.Add(data);

        return data;
    }

    // 接受 request body，用來示範 fingerprint mismatch (422) 情境
    [HttpPost]
    [Idempotent]
    public async Task<ActionResult<WeatherForecast>> PostWithBody(
        [FromBody] CreateWeatherRequest request,
        CancellationToken cancel = default)
    {
        var rng = new Random();
        var data = new WeatherForecast
        {
            TemperatureC = request.TemperatureC,
            Summary =  Summaries[rng.Next(Summaries.Length)],
            Date = DateTime.UtcNow
        };
        s_repository.Add(data);

        return data;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> Get()
    {
        return s_repository;
    }
}

public record CreateWeatherRequest(int TemperatureC);
