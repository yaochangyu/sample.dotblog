using MassTransit;
using Message;

namespace Consumer2;

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        Console.WriteLine($"Order received: {context.Message.OrderId} at {context.Message.Timestamp}");
        return Task.CompletedTask;
    }
}