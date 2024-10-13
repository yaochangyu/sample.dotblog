using MassTransit;
using Message;
using Microsoft.Extensions.Hosting;

public class MessagePublishService : IHostedService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MessagePublishService(IPublishEndpoint publishEndpoint)
    {
        this._publishEndpoint = publishEndpoint;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 使用 Publish 發佈事件，所有訂閱者都能接收此事件
        await this._publishEndpoint.Publish(new OrderSubmitted
        {
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        Console.WriteLine("OrderSubmitted event published.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}