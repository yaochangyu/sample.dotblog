using System;
using Lab.NetRemoting.Core;

namespace Lab.NetRemoting.Implement
{
    public class TrMessage : MarshalByRefObject, ITrMessage
    {
        private readonly string _name = "yao";

        public TrMessage()
        {
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss}, Create Construct");
        }

        public TrMessage(string name)
        {
            this._name = name;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss}, Create Construct pass {name}");
        }

        public string GetName()
        {
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss}, Call GetName Method");
            return this._name;
        }

        public DateTime GetNow()
        {
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss}, Call GetNow Method");
            return DateTime.Now;
        }

        public Person GetPerson()
        {
            var person = new Person
            {
                Name   = Faker.Name.FullName(),
                Gender = Faker.Boolean.Random() ? "M" : "F",
                Age    = Faker.RandomNumber.Next(1, 100)
            };
            return person;
        }
    }
}