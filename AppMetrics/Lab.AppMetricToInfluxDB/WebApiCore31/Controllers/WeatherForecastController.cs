using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiCore31.Controllers
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
        private readonly IMetrics                           _metrics;

        public WeatherForecastController(IMetrics metrics)
        {
            this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        //public WeatherForecastController(ILogger<WeatherForecastController> logger)
        //{
        //    this._logger = logger;
        //}

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            using (this._metrics.Measure.Timer.Time(AppMetricsRegistery.ReservoirsMetrics.TimerUsingAlgorithmRReservoir)
            )
            {
                await this.Delay();
            }

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                             {
                                 Date         = DateTime.Now.AddDays(index),
                                 TemperatureC = rng.Next(-20, 55),
                                 Summary      = Summaries[rng.Next(Summaries.Length)]
                             })
                             .ToArray();
        }

        private Task Delay()
        {
            var second = DateTime.Now.Second;

            if (second <= 20)
            {
                return Task.CompletedTask;
            }

            if (second <= 40)
            {
                return Task.Delay(TimeSpan.FromMilliseconds(50), this.HttpContext.RequestAborted);
            }

            return Task.Delay(TimeSpan.FromMilliseconds(100), this.HttpContext.RequestAborted);
        }
    }
}