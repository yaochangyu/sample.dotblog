using System;
using Microsoft.Owin.Hosting;
using NLog;

namespace Server1
{
    internal class Program
    {
        private static readonly ILogger s_logger;

        static Program()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        private static void Main(string[] args)
        {
            var url = "https://localhost:44378/";

            //var url = "http://localhost:9527"; 
            using (WebApp.Start<Startup>(url))
            {
                s_logger.Trace($"Web API Start,URL:{url}");

                Console.ReadLine();
            }
        }
    }
}