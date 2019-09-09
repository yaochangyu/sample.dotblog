using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server1.Controllers
{
    public class TokenCancellationApiController : ApiController
    {
        private static readonly object _lock = new object();
        public static           string _lastError;

        // Static types will mean that you can only run 
        // one long running process at a time.
        // If more than 1 needs to run, you will have to 
        // make them instance variable and manage 
        // threading and lifecycle
        private static CancellationTokenSource cTokenSource;
        private static CancellationToken       cToken;

        [HttpGet]
        [Route("api/TokenCancellationApi/BeginLongProcess/{seconds}")]
        public string BeginLongProcess(int seconds)
        {
            //Lock and check if process has already started or not.
            lock (_lock)
            {
                if (null != cTokenSource)
                {
                    return "A long running is already underway.";
                }

                cTokenSource = new CancellationTokenSource();
            }

            //if running asynchronously
            var task = Task.Factory.StartNew(() => LongRunningFunc(cTokenSource.Token, seconds));
            task.ContinueWith(Cleanup);
            return "Long running process has started!";

            //if running synchronusly
            string msg;
            try
            {
                LongRunningFunc(cTokenSource.Token, seconds);
            }
            catch (OperationCanceledException)
            {
                msg = "The running process has been cancelled";
                Console.WriteLine(msg);
                return msg;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return ex.Message;
            }
            finally
            {
                Cleanup(null);
            }

            msg = "The running process has been cancelled";
            Console.WriteLine(msg);

            return msg;
        }

        [HttpGet]
        [Route("api/TokenCancellationApi/CancelLongProcess")]
        public string CancelLongProcess()
        {
            // cancelling task
            string msg;
            if (null != cTokenSource)
            {
                lock (_lock)
                {
                    if (null != cTokenSource)
                    {
                        cTokenSource.Cancel();
                    }

                    msg = "Cancellation Requested";
                    Console.WriteLine(msg);
                    return msg;
                }
            }

            msg = "Long running task already completed";

            Console.WriteLine(msg);
            return msg;
        }

        [HttpGet]
        [Route("api/TokenCancellationApi/GetLastError")]
        public string GetLastError()
        {
            return string.IsNullOrEmpty(_lastError) ? "No Error" : _lastError;
        }

        private static void Cleanup(Task task)
        {
            if (null != task && task.IsFaulted)
            {
                Debug.WriteLine("Error encountered while running task");
                _lastError = task.Exception.GetBaseException().Message;
            }

            lock (_lock)
            {
                if (null != cTokenSource)
                {
                    cTokenSource.Dispose();
                }

                cTokenSource = null;
            }
        }

        private static void LongRunningFunc(CancellationToken token, int seconds)
        {
            Debug.WriteLine("Long running method");
            var j = 0;

            //Long running loop should always check if cancellation requested.
            while (!token.IsCancellationRequested && j < seconds)
            {
                //Wait on token instead of deterministic sleep
                //This way, thread will wakeup as soon as canellation
                //is requested even if sleep time hasn't elapsed.
                //Waiting 5 seconds
                Console.WriteLine(j.ToString());
                token.WaitHandle.WaitOne(5000);
                j++;
            }

            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            Debug.WriteLine("Done looping");
        }
    }
}