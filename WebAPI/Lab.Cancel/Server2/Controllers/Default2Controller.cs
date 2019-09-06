using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using NLog;

namespace Server1.Controllers
{
    [RoutePrefix("api/books")]
    public class Default2Controller : ApiController
    {
        private static readonly ILogger                 s_logger;
        private readonly        CancellationTokenSource _cancel;

        static Default2Controller()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        public Default2Controller()
        {
            if (this._cancel == null)
            {
                this._cancel = new CancellationTokenSource();
            }
        }

        //[HttpGet]
        [Route("orders")]

        //[Route("bg")]
        public async Task<IHttpActionResult> Cancel()
        {
            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        // GET api/default
        [Route("")]
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
    }
}