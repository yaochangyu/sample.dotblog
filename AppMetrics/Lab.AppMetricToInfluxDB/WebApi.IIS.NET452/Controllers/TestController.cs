using System;
using System.Web.Http;
using App.Metrics;
using WebApi.IIS.NET452.Infrastructure;

namespace WebApi.IIS.NET452.Controllers
{
    [RoutePrefix("v1/test")]
    public class TestController : ApiController
    {
        private readonly IMetrics _metrics;

        public TestController()
        {

        }

        public TestController(IMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            this._metrics = metrics;
        }

        [Route("")]
        [HttpGet]
        public IHttpActionResult Get()
        {
            this._metrics.Measure.Counter.Increment(SampleMetrics.BasicCounter);

            return this.Ok("testing");
        }
    }
}