﻿using Consumer;
using MassTransit;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<OrderSubmittedConsumer>();

                    x.UsingRabbitMq((context, config) =>
                    {
                        config.Host("rabbitmq://localhost", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });

                        // 設置接收端點，並消費 `OrderSubmitted`
                        config.ReceiveEndpoint("order-submitted-queue", endpoint =>
                        {
                            endpoint.ConfigureConsumer<OrderSubmittedConsumer>(context);
                        });
                    });
                });

                services.AddMassTransitHostedService();
            })
            .Build();
        try
        {
            Console.WriteLine("Listening for OrderSubmitted events...");
            await host.RunAsync();
        }
        finally
        {
            await host.StopAsync();
        }
    }
}