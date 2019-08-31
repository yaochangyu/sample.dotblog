using System;
using System.Timers;

namespace ConsoleApp1
{
    public class DoThing
    {
        private readonly Timer _timer;

        public DoThing()
        {
            this._timer         =  new Timer(1000) { AutoReset = true };
            this._timer.Elapsed += (sender, eventArgs) => Console.WriteLine($"Now Time:{DateTime.Now}");
        }

        public void Start()
        {
            this._timer.Start();
            Console.WriteLine($"Timer Start");
        }

        public void Stop()
        {
            this._timer.Stop();
            Console.WriteLine($"Timer Stop");
        }
    }
}