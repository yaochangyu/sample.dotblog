using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace UnitTestProject1
{
    [TestClass]
    public class Queue
    {
        [TestMethod]
        public void Sender()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "hello",
                                     basicProperties: null,
                                     body: body);
                Console.WriteLine("Sent {0}", message);
            }

        }

        [TestMethod]
        public void Receive()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, args) =>
                                     {
                                         var body = args.Body;
                                         var message = Encoding.UTF8.GetString(body);
                                         Console.WriteLine("Received {0}", message);
                                     };
                channel.BasicConsume(queue: "hello",
                                     autoAck: true,
                                     consumer: consumer);

            }

        }

    }
}
