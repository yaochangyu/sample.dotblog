using System;
using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ContainerBuilder cb = new ContainerBuilder();

            cb.RegisterType<EnglishHello>().Keyed<IHello>("EN");
            cb.RegisterType<FrenchHello>().Keyed<IHello>("FR");
            cb.RegisterType<HelloConsumer>().WithAttributeFiltering();
            var container = cb.Build();

            var consumer = container.Resolve<HelloConsumer>();
            Console.WriteLine(consumer.SayHello());
        }
    }
}