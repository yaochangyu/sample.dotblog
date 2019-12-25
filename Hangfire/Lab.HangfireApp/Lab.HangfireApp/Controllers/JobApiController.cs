using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;

namespace Lab.HangfireApp.Controllers
{
    public class JobApiController : ApiController
    {
        public async Task<IHttpActionResult> Post(string content)
        {
      
            BackgroundJob.Enqueue(() => Send(content,null));
            return this.Ok();
        }

        public static void Send(string message,  PerformContext context)
        {
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine("Hello, world!");
            Thread.Sleep(10000);
            Trace.WriteLine($"由Hangfire發送的訊息:{message}, 時間:{DateTime.Now}");
        }
    }
}