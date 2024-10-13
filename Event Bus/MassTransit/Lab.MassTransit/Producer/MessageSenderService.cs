using MassTransit;
using Message;
using Microsoft.Extensions.Hosting;

namespace Producer;

public class MessageSenderService : IHostedService
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MessageSenderService(ISendEndpointProvider sendEndpointProvider)
    {
        this._sendEndpointProvider = sendEndpointProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 取得 SendEndpoint 並發送訊息到指定佇列
        var sendEndpoint =

            // await this._sendEndpointProvider.GetSendEndpoint(new Uri("rabbitmq://localhost/order-submitted-queue"));
            await this._sendEndpointProvider.GetSendEndpoint(new Uri("rabbitmq://localhost/order-submitted-queue"));

        await sendEndpoint.Send(new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        Console.WriteLine("OrderSubmitted event sent to order-submitted-queue.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}