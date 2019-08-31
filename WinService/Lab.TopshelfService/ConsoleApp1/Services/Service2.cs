using System;
using System.Timers;
using NLog;

namespace ConsoleApp1.Services
{
    internal class Service2 : IService
    {
        private static readonly ILogger s_logger;
        private readonly        Timer   _timer;

        static Service2()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        public Service2()
        {
            this._timer         =  new Timer(1000) {AutoReset = true};
            this._timer.Elapsed += (sender, eventArgs) => Console.WriteLine($"I'm Service2");
        }

        public void Start()
        {
            this._timer.Start();
            s_logger.Trace("Timer Start");
        }

        public void Stop()
        {
            this._timer.Stop();
            s_logger.Trace("Timer Stop");
        }
    }
}