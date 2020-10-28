using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            using (this._logger.BeginScope($"Scope Id:{Guid.NewGuid()}"))
            //using (this._logger.BeginScope("Scope Id:{id}", Guid.NewGuid().ToString())
            {
                this._logger.LogInformation("開始，訪問 WeatherForecast api");
                this._logger.LogInformation("執行流程");

                var random = new Random();
                var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                                       {
                                           Date         = DateTime.Now.AddDays(index),
                                           TemperatureC = random.Next(-20, 55),
                                           Summary      = Summaries[random.Next(Summaries.Length)]
                                       })
                                       .ToArray();

                this._logger.LogInformation("結束流程");

                return this.Ok(result);
            }
        }
    }
}