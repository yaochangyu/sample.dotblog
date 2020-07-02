using System;
using NETCore.Behaviors;
using PowerArgs;

namespace NETCore
{
    internal class Program
    {
        static Program()
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void Main(string[] args)
        {
            Args.InvokeAction<MultipleBehavior>(args);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;

            // todo:write log
            Console.WriteLine(exception.ToString());
        }
    }
}