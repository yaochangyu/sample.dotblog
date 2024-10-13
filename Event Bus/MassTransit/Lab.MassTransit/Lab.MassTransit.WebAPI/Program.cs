using Lab.MassTransit.WebAPI;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    // 註冊消費者
    x.AddConsumer<OrderCreatedConsumer>();

    // 配置 MassTransit 使用 RabbitMQ
    x.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        // 註冊消費者
        config.ReceiveEndpoint("order/created/event", e => { e.ConfigureConsumer<OrderCreatedConsumer>(context); });
    });
});
builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 使用中介軟體和路由
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.UseHttpsRedirection();

app.Run();