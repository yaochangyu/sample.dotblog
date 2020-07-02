using System;
using NetCore31.Behaviors;
using PowerArgs;

namespace NetCore31
{
    internal class Program
    {
        static Program()
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public static void Main(string[] args)
        {
            ////解析參數
            //var copyFileRequest = Args.Parse<CopyFileRequest>(args);
            //Console.WriteLine(JsonConvert.SerializeObject(copyFileRequest));

            ////綁定單一行為
            //Args.InvokeMain<SingleBehavior>(args);

            //綁定多個行為
            Args.InvokeAction<MultipleBehavior>(args);

            ////解析行為
            //var argAction = Args.ParseAction<MultipleBehavior>(args);

            //Console.ReadLine();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
            Console.WriteLine(exception.Message);
        }
    }
}