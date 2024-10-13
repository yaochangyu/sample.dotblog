namespace Lab.MassTransit.Producer.Order;

// 訂單請求
public class CreateOrderRequest
{
    public decimal TotalAmount { get; set; }
}