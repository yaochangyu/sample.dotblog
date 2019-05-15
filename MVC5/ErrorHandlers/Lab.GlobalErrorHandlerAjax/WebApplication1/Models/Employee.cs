using System;

namespace WebApplication1.Models
{
    internal class Employee
    {
        public Employee()
        {
        }

        public Guid   Id   { get; set; }
        public string Name { get; set; }
        public int    Age  { get; set; }
    }
}