using MassTransit;

namespace Lab.MassTransit.Producer.Order;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        Console.WriteLine($"Order created: {context.Message.OrderId}, Total Amount: {context.Message.TotalAmount}");
        return Task.CompletedTask;
    }
}