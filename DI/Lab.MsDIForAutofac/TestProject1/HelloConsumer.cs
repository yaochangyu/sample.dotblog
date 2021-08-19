using System;
using Autofac.Features.AttributeFilters;

namespace TestProject1
{
    public class HelloConsumer
    {
        private readonly IHello helloService;

        public HelloConsumer([KeyFilter("FR")] IHello helloService)
        {
            if (helloService == null)
            {
                throw new ArgumentNullException("helloService");
            }
            this.helloService = helloService;
        }

        public string SayHello()
        {
            return this.helloService.SayHello();
        }
    }
}