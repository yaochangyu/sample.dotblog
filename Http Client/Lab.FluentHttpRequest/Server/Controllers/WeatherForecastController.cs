using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
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
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get(int lessTemperature)
        {
            var rng = new Random();
            var sources = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                                             {
                                                 Date         = DateTime.Now.AddDays(index),
                                                 TemperatureC = rng.Next(-20, 55),
                                                 Summary      = Summaries[rng.Next(Summaries.Length)]
                                             })
                                             .ToArray();
            var content       = sources.Where(p => p.TemperatureC < lessTemperature).ToList();
            
            var result = new ObjectResult(content)
            {
                StatusCode = 500,
            };
            return result;
            // return response;
            throw new Exception("gg");
            
        }
    }
}