﻿using MassTransit;
using Message;
using Microsoft.Extensions.Hosting;

public class MessagePublishService2 : IHostedService
{
    private readonly IBus _bus;

    public MessagePublishService2(IBus bus)
    {
        this._bus = bus;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var orderSubmitted = new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };
        // ===Publish===
        // 使用 Publish 發佈事件，所有訂閱者都能接收此事件
        await this._bus.Publish(orderSubmitted, cancellationToken);
        Console.WriteLine("OrderSubmitted event published.");
        
        // ===Send===
        // EndpointConvention.Map<OrderSubmitted>(new Uri("queue:order-submitted-queue"));
        EndpointConvention.Map<OrderSubmitted>(new Uri("rabbitmq://localhost/order-submitted-queue"));

        await this._bus.Send(orderSubmitted, cancellationToken);
        Console.WriteLine("OrderSubmitted event sent.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}