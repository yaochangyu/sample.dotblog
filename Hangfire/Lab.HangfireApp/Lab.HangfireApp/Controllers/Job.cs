using System;
using System.Diagnostics;
using System.Threading;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;

namespace Lab.HangfireApp.Controllers
{
    internal class Job
    {
        //[Queue("a1")]
        public static void LongRunning(IJobCancellationToken cancellationToken)
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (cancellationToken.ShutdownToken.IsCancellationRequested)
                {
                    Trace.WriteLine("Task Cancel");
                }

                cancellationToken.ThrowIfCancellationRequested();

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        //[Queue("a2")]
        public static void LongRunning2(IJobCancellationToken cancellationToken)
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (cancellationToken.ShutdownToken.IsCancellationRequested)
                {
                    Trace.WriteLine("Task Cancel");
                }

                cancellationToken.ThrowIfCancellationRequested();

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public static void Send(string message)
        {
            //Thread.Sleep(10000);
            Trace.WriteLine($"Message:{message}, Now:{DateTime.Now}");
        }

        public static void Send(string message, IJobCancellationToken cancelToken)
        {
            //Thread.Sleep(10000);
            Trace.WriteLine($"Message:{message}, Now:{DateTime.Now}");
        }

        public static void Send(string message, PerformContext context, IJobCancellationToken cancelToken)
        {
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine($"Message:{message}, Now:{DateTime.Now}");
            //Thread.Sleep(10000);
            Trace.WriteLine($"Message:{message}, Now:{DateTime.Now}");
        }
    }
}