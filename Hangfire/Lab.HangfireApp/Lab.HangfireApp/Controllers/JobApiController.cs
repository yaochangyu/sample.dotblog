using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Hangfire;
using Hangfire.Storage;

namespace Lab.HangfireApp.Controllers
{
    public class JobApiController : ApiController
    {
        public async Task<IHttpActionResult> Post(string content)
        {
            BackgroundJob.Enqueue(() => Job.LongRunning(JobCancellationToken.Null));

            ////立即執行一次
            //BackgroundJob.Enqueue(() => Job.Send(content, null));

            ////延遲執行一次
            //BackgroundJob.Schedule(() => Job.Send(content, null), TimeSpan.FromSeconds(3));

            ////定期執行多次
            //RecurringJob.AddOrUpdate(() => Job.Send(content, null), Cron.Daily(0, 1));
            //RecurringJob.AddOrUpdate(() => Job.Send(content, null), Cron.Minutely);
            //RecurringJob.AddOrUpdate(() => Job.Send(content, null), "0 12 * * 2");

            ////接續Job執行
            //var masterId = BackgroundJob.Enqueue(() => Job.Send(content, null));
            //BackgroundJob.ContinueJobWith(masterId, () => Job.Send("Continue Job", null));

            return this.Ok();
        }
    }
}