using System;

namespace WebApiOwinNet48
{
    public class Commander
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Get()
        {
            var msg = "GG";
            Console.WriteLine(msg);
            return msg;
        }
    }
}