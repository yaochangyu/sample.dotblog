using MassTransit;
using Message;

namespace Consumer2;

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var destinationAddress = new Uri("rabbitmq://localhost/order-submitted-queue");
        var command = new OrderSubmitted()
        {
            OrderId = context.Message.OrderId,
            Timestamp = context.Message.Timestamp
        };

        await context.Send(destinationAddress, command);

        // var endpoint = await context.GetSendEndpoint(destinationAddress);
        // await endpoint.Send(command);
        Console.WriteLine($"Order received: {context.Message.OrderId} at {context.Message.Timestamp}");
    }
}