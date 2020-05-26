using System;

namespace ConsoleApp
{
    public class ConsoleProvider : IProvider
    {
        private readonly IMessage _message;

        public ConsoleProvider(IMessage message)
        {
            this._message = message;
        }

        public void Log(string msg)
        {
            this._message.Write();
            Console.WriteLine(msg);
        }
    }
}