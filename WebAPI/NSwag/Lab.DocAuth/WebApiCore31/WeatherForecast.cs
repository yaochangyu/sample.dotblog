using System;

namespace WebApiCore31
{
    public class WeatherForecast
    {
        /// <summary>
        /// ¤é´Á
        /// </summary>
        /// <example>2019/01/02</example>
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Â²¤¶
        /// </summary>
        /// <example>Summary</example>
        public string Summary { get; set; }
    }
}
