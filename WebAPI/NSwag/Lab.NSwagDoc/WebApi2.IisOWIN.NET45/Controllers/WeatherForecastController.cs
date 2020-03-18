using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApi2.IisOWIN.NET45.Controllers
{
    public class WeatherForecastController : ApiController
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get(string name, CancellationToken cancel)
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                             {
                                 Date         = DateTime.Now.AddDays(index),
                                 TemperatureC = rng.Next(-20, 55),
                                 Summary      = Summaries[rng.Next(Summaries.Length)]
                             })
                             .ToArray();
        }

        [HttpPost]
        public async Task<IEnumerable<WeatherForecast>> Post(WeatherForecast content, CancellationToken cancel)
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                             {
                                 Date         = DateTime.Now.AddDays(index),
                                 TemperatureC = rng.Next(-20, 55),
                                 Summary      = Summaries[rng.Next(Summaries.Length)]
                             })
                             .ToArray();
        }
    }
}