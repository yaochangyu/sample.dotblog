using System;
using System.Threading;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Polly;

namespace Lab.HangfireServer.Jobs
{
    public class Job
    {
        [AutomaticRetry(Attempts           = 3,
                        DelaysInSeconds    = new[] {5, 10, 15},
                        OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(30)]
        public void AutoRetry(string msg, PerformContext consoleLog, IJobCancellationToken cancelToken)
        {
            if (msg == "1")
            {
                consoleLog.WriteLine($"執行中，目前時間：{DateTime.Now}");
                Thread.Sleep(5000);
                throw new Exception("噴錯了~");
            }

            consoleLog.WriteLine($"執行完畢，目前時間：{DateTime.Now}");
        }

        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(120)]
        public void PollyRetry(string msg, PerformContext consoleLog, IJobCancellationToken cancelToken)
        {
            var retryPolicy = Policy.Handle<Exception>()
                                    .WaitAndRetry(new[]
                                                  {
                                                      TimeSpan.FromSeconds(5),
                                                      TimeSpan.FromSeconds(10),
                                                      TimeSpan.FromSeconds(15)
                                                  },
                                                  (exception, retryTime, context) =>
                                                  {
                                                      consoleLog.WriteLine($"延遲重試：{retryTime}，目前時間：{DateTime.Now}");
                                                  });
            retryPolicy.Execute(() => this.PollyAction(msg, consoleLog, cancelToken));
            consoleLog.WriteLine($"執行完畢，目前時間：{DateTime.Now}");
        }

        [DisableConcurrentExecution(5)]
        public void WaitTimeout(string msg, PerformContext context, IJobCancellationToken cancelToken)
        {
            if (msg == "1")
            {
                context.WriteLine($"執行中，目前時間：{DateTime.Now}");
                Thread.Sleep(60000);
            }

            context.WriteLine($"執行完畢：目前時間{DateTime.Now}");
        }

        private void PollyAction(string msg, PerformContext consoleLog, IJobCancellationToken cancelToken)
        {
            if (msg == "1")
            {
                consoleLog.WriteLine($"PollyAction 執行中，目前時間：{DateTime.Now}");
                Thread.Sleep(5000);
                throw new Exception("噴錯了~");
            }

            consoleLog.WriteLine($"執行完畢，目前時間：{DateTime.Now}");
        }
    }
}