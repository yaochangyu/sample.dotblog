using System;

namespace ConsoleApp
{
    public class ConsoleMessage : IMessage
    {
        public int Age { get; set; }

        public string Name { get; set; }

        public ConsoleMessage(string name, int age)
        {
            this.Name = name;
            this.Age  = age;
        }

        public void Write()
        {
            Console.WriteLine($"Name：{this.Name}，Age：{this.Age}");
        }
    }
}