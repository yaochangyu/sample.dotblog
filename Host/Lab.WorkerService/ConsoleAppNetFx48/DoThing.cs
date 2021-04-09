// using System;
// using System.Timers;
// using NLog;
//
// namespace ConsoleAppNetFx48
// {
//     public class DoThing
//     {
//         private static readonly ILogger s_logger;
//         private readonly        Timer   _timer;
//
//         static DoThing()
//         {
//             if (s_logger == null)
//             {
//                 s_logger = LogManager.GetCurrentClassLogger();
//             }
//         }
//
//         public DoThing()
//         {
//             this._timer         =  new Timer(1000) {AutoReset = true};
//             this._timer.Elapsed += (sender, eventArgs) => Console.WriteLine($"Now Time:{DateTime.Now}");
//         }
//
//         public void Start()
//         {
//             this._timer.Start();
//             s_logger.Trace("Timer Start");
//         }
//
//         public void Stop()
//         {
//             this._timer.Stop();
//             s_logger.Trace("Timer Stop");
//         }
//     }
// }