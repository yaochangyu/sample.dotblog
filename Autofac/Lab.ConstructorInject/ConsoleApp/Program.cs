using System;
using Autofac;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ConsoleMessage>().As<IMessage>()
                   .WithParameter("name", "yao")
                   .WithParameter("age",  18);

            builder.RegisterType<ConsoleProvider>().As<IProvider>()
                   .UsingConstructor(typeof(IMessage));

            var container = builder.Build();

            var message = container.Resolve<IMessage>();

            message.Write();

            var provider = container.Resolve<IProvider>();
            provider.Log("Inject Message");

            //覆蓋建構子
            var messageParameter = container.Resolve<IMessage>(
                                                               new NamedParameter("name", "Oh..No"),
                                                               new NamedParameter("age",  77));

            // 執行取得物件的方法
            messageParameter.Write();

            Console.WriteLine("Press any key for continuing...");
            Console.ReadKey();
        }
    }
}