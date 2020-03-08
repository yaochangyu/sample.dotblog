using System.Web.Http;
using Hangfire;
using Hangfire.States;
using Lab.HangfireServer.Jobs;

namespace Lab.HangfireServer.Controllers
{
    [RoutePrefix("api/default")]
    public class DefaultController : ApiController
    {
        private readonly BackgroundJobClient _client;
        private readonly Job                 _job;

        public DefaultController()
        {
            this._client = new BackgroundJobClient();
            this._job    = new Job();
        }

        [HttpGet]
        [Route("AutoRetry")]
        public void AutoRetry(string msg)
        {
            this._client.Enqueue(() => this._job.AutoRetry(msg, null, null));
        }

        [Route("SpecifyQueue")]
        public void Get(string msg, string queueName)
        {
            IState state = new EnqueuedState(queueName);
            this._client.Create(Hangfire.Common.Job.FromExpression(() => this._job.AutoRetry(msg, null, null)), state);
            this._client.Enqueue(() => this._job.AutoRetry(msg, null, null));
        }

        [HttpGet]
        [Route("PollyRetry")]
        public void PollyRetry(string msg)
        {
            this._client.Enqueue(() => this._job.PollyRetry(msg, null, null));
        }
    }
}