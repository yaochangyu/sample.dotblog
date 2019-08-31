using System;
using System.Timers;
using NLog;

namespace ConsoleApp1
{
    public class DoThing
    {
        private readonly Timer _timer;
        private static ILogger s_logger;

        static DoThing()
        {
            if (s_logger==null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }
        public DoThing()
        {
            this._timer         =  new Timer(1000) { AutoReset = true };
            this._timer.Elapsed += (sender, eventArgs) => Console.WriteLine($"Now Time:{DateTime.Now}");
        }

        public void Start()
        {
            this._timer.Start();
            s_logger.Trace($"Timer Start");
        }

        public void Stop()
        {
            this._timer.Stop();
            s_logger.Trace($"Timer Stop");
        }
    }
}