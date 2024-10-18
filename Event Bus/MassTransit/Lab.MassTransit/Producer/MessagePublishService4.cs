using MassTransit;
using Message;
using Microsoft.Extensions.Hosting;

public class MessagePublishService4 : IHostedService
{
    
    private readonly IBusControl busControl;

    public MessagePublishService4(IBusControl bus)
    {
        this.busControl = bus;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // var busControl = Bus.Factory.CreateUsingRabbitMq(config =>
        // {
        //     config.Host("rabbitmq://localhost", h =>
        //     {
        //         h.Username("guest");
        //         h.Password("guest");
        //     });
        // });
        await busControl.StartAsync(cancellationToken);
        
        var orderSubmitted = new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };

        // 發佈事件
        await busControl.Publish(new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        }, cancellationToken); 

        // ===Publish===
        // 使用 Publish 發佈事件，所有訂閱者都能接收此事件
        await busControl.Publish(orderSubmitted, cancellationToken);
        Console.WriteLine("OrderSubmitted event published.");

        // ===Send===
        EndpointConvention.Map<OrderSubmitted>(new Uri("rabbitmq://localhost/order-submitted-queue"));
        await busControl.Send(orderSubmitted, cancellationToken);
        Console.WriteLine("OrderSubmitted event sent.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}