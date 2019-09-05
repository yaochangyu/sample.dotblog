using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using NLog;

namespace Server.Controllers
{
    public class DefaultController : ApiController
    {
        private static readonly ILogger s_logger;

        static DefaultController()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        // GET api/default
        public async Task<IHttpActionResult> Get(CancellationToken cancel)
        {
            var index = 0;
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    cancel.ThrowIfCancellationRequested();

                    await Task.Delay(1000, cancel);
                    index = i + 1;
                }

                s_logger.Trace("Process Done");

                return this.Ok($"{index}");
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                s_logger.Trace("Process Cancel");
            }

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        private async Task<string> GetDataAsync(CancellationToken cancel)
        {
            return await Task.Run(() =>
                                  {
                                      long n = 0;
                                      while (!cancel.IsCancellationRequested)
                                      {
                                          Console.WriteLine(n);
                                          n = n + 1;
                                          Task.Delay(1000, cancel);
                                      }

                                      return $"{n}";
                                  }, cancel);
        }
    }
}