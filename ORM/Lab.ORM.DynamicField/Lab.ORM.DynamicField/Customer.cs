using System.Text.Json.Serialization;

namespace Lab.ORM.DynamicField;

public record Customer
{
    public string Name { get; set; }

    public int Age { get; set; }

    public Order[] Orders { get; set; }

    public Product Product { get; set; }
}

public record Order
{
    // [JsonPropertyName("OrderPrice")]
    public decimal Price { get; set; }

    public string ShippingAddress { get; set; }
}

public record Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}