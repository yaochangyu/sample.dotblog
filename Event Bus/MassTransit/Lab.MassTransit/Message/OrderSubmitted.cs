namespace Message;

public class OrderSubmitted
{
    public Guid OrderId { get; set; }
    public DateTime Timestamp { get; set; }
}