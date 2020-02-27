using System;
using System.Diagnostics;

namespace Lab.HangfireManager
{
    internal class Job
    {
        public static void Send(string message)
        {
            //Thread.Sleep(10000);
            Trace.WriteLine($"Message:{message}, Now:{DateTime.Now}");
        }
    }
}