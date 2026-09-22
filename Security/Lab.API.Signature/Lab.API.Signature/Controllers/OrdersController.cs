using Microsoft.AspNetCore.Mvc;

namespace Lab.API.Signature.Controllers;

/// <summary>
/// 示範被 <see cref="Middleware.SignatureAuthenticationMiddleware"/> 保護的端點。
/// 純教學展示，回傳假資料，不接 EF Core 訂單資料表。
/// </summary>
[ApiController]
[Route("api/protected/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        var order = new
        {
            id,
            status = "Paid",
            amount = 1000
        };

        return Ok(order);
    }

    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = new
        {
            orderId = Guid.NewGuid().ToString("N"),
            request.ProductName,
            request.Amount,
            status = "Created"
        };

        return Ok(order);
    }
}

public record CreateOrderRequest(string ProductName, decimal Amount);
