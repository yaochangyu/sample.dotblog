﻿using MassTransit;
using Message;
using Microsoft.Extensions.Hosting;

public class MessagePublishService3 : IHostedService
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MessagePublishService3(ISendEndpointProvider sendEndpointProvider)
    {
        this._sendEndpointProvider = sendEndpointProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var orderSubmitted = new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };
        
        // EndpointConvention.Map<OrderSubmitted>(new Uri("queue:order-submitted-queue"));
        // var uri = new Uri("rabbitmq://localhost/order-submitted-queue");
        var uri = new Uri("queue:order-submitted-queue");
        var endpoint = await this._sendEndpointProvider.GetSendEndpoint(uri);
        await endpoint.Send(orderSubmitted, cancellationToken);
        Console.WriteLine("OrderSubmitted event sent.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}