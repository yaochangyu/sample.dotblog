using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Producer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    // 配置 MassTransit 使用 RabbitMQ
                    x.UsingRabbitMq((context, config) =>
                    {
                        config.Host("localhost", "/", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });
                    });
                });

                // 將 MessageSenderService 註冊為 IHostedService
                // services.AddHostedService<MessageSenderService>();
                services.AddHostedService<MessagePublishService3>();
            })
            .Build();

        await host.StartAsync();
    }
}