using System;
using System.Threading.Tasks;
using App.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCore31.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        private readonly IMetrics _metrics;

        public TestController(IMetrics metrics)
        {
            this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        [HttpGet("exponentially-decaying")]
        public async Task<string> ExponentiallyDecaying()
        {
            using (this._metrics.Measure.Timer.Time(AppMetricsRegistery.ReservoirsMetrics
                                                                       .TimerUsingExponentialForwardDecayingReservoir))
            {
                await this.Delay();
            }

            return "OK";
        }

        [HttpGet("exponentially-decaying-low-weight")]
        public async Task<string> ExponentiallyDecayingLowWeight()
        {
            using (this._metrics.Measure.Timer.Time(AppMetricsRegistery.ReservoirsMetrics
                                                                       .TimerUsingForwardDecayingLowWeightThresholdReservoir))
            {
                await this.Delay();
            }

            return "OK";
        }

        [HttpGet("sliding-window")]
        public async Task<string> SlidingWindow()
        {
            using (this._metrics.Measure.Timer.Time(AppMetricsRegistery.ReservoirsMetrics
                                                                       .TimerUsingSlidingWindowReservoir))
            {
                await this.Delay();
            }

            return "OK";
        }

        [HttpGet("uniform")]
        public async Task<string> Uniform()
        {
            using (this._metrics.Measure.Timer.Time(AppMetricsRegistery.ReservoirsMetrics
                                                                       .TimerUsingAlgorithmRReservoir)
            )
            {
                await this.Delay();
            }

            return "OK";
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