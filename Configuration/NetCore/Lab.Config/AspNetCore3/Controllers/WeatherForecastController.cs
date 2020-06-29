using System;
using System.Collections.Generic;
using System.Linq;
using Lab.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore3.Controllers
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
        private IConfiguration _config;
        private AppSetting _appSetting;
        private Player _player1;
        private Player _player2;

        // TODO:依賴 IOptions<AppSetting> 
        //public WeatherForecastController(IOptions<AppSetting> options)
        //{
        //    this._appSetting = options.Value;
        //}

        // TODO:依賴 IOptionsSnapshot<Player> 
        //public WeatherForecastController(IOptionsSnapshot<Player> options)
        //{
        //    this._player1 = options.Get("Player1");
        //    this._player2 = options.Get("Player2");
        //}

        // TODO:依賴 IOptionsMonitor<Player> 
        public WeatherForecastController(IOptionsMonitor<Player> options)
        {
            this._player1 = options.Get("Player1");
            this._player2 = options.Get("Player2");
        }

        // TODO:依賴 IConfiguration
        //public WeatherForecastController(IConfiguration config)
        //{
        //    this._config = config;
        //}

        //public WeatherForecastController(IOptions<AppSetting> options, IConfiguration config)
        //{
        //    this._config = config;
        //    this._appSetting = options.Value;
        //}

        //public WeatherForecastController(ILogger<WeatherForecastController> logger)
        //{
        //    _logger = logger;
        //}

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
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