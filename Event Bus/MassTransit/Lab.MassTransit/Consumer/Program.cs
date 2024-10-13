using Consumer;
using MassTransit;

public class Program
{
    public static async Task Main(string[] args)
    {
        var busControl = Bus.Factory.CreateUsingRabbitMq(config =>
        {
            config.Host("rabbitmq://localhost", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            config.ReceiveEndpoint("order-submitted-queue", ep =>
            {
                ep.Consumer<OrderSubmittedConsumer>();
            });
        });

        await busControl.StartAsync();
        try
        {
            Console.WriteLine("Listening for OrderSubmitted events...");
            await Task.Run(Console.ReadLine);
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}