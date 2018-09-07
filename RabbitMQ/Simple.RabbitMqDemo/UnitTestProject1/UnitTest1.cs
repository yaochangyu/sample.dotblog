using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Sender()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                //声明queue
                channel.QueueDeclare(queue: "hello",//队列名
                                     durable: false,//是否持久化
                                     exclusive: false,//true:排他性，该队列仅对首次申明它的连接可见，并在连接断开时自动删除
                                     autoDelete: false,//true:如果该队列没有任何订阅的消费者的话，该队列会被自动删除
                                     arguments: null);//如果安装了队列优先级插件则可以设置优先级

                string message = "Hello World!";//待发送的消息
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",//exchange名称
                                     routingKey: "hello",//如果存在exchange,则消息被发送到名称为hello的queue的客户端
                                     basicProperties: null,
                                     body: body);//消息体
                Console.WriteLine(" [x] Sent {0}", message);
            }

        }

        [TestMethod]
        public void Receive()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",//指定发送消息的queue，和生产者的queue匹配
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                                     {
                                         var body = ea.Body;
                                         var message = Encoding.UTF8.GetString(body);
                                         Console.WriteLine(" [x] Received {0}", message);
                                     };
                channel.BasicConsume(queue: "hello",
                                     autoAck: true,//和tcp协议的ack一样，为false则服务端必须在收到客户端的回执（ack）后才能删除本条消息
                                     consumer: consumer);

            }

        }

    }
}
