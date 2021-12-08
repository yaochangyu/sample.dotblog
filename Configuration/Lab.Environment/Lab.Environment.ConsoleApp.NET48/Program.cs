
using System;

namespace Lab.Environment.ConsoleApp.NET48
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var appEnv = System.Environment.GetEnvironmentVariable("APP_ENV");
            var scoopPath = System.Environment.GetEnvironmentVariable("scoop");

            if (string.IsNullOrWhiteSpace(appEnv) == false)
            {
                Console.WriteLine(appEnv);
            }
        }
    }
}