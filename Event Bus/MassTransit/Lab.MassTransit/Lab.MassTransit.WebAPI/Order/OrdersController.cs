using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Lab.MassTransit.WebAPI.Order;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    public OrdersController(IPublishEndpoint publishEndpoint)
    {
        this._publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request == null)
        {
            return this.BadRequest("Invalid order data");
        }

        var orderCreatedEvent = new OrderCreated
        {
            OrderId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            TotalAmount = request.TotalAmount
        };
        
        // 生產者，發布 OrderCreated 事件
        await this._publishEndpoint.Publish(orderCreatedEvent);

        return this.Ok($"Order created with ID: {orderCreatedEvent.OrderId}");
    }
}