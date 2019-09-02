using System;

namespace Server
{
    public class MyService : IService
    {
        public string GetName()
        {
            Console.WriteLine("MyService");
            return "MyService";
        }
    }
}