using MessagePack;
using MemoryPack;

namespace Lab.HybirdCache.Compress.Models;

[MessagePackObject]
[MemoryPackable]
public partial class ProductModel
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [Key(2)]
    public string Description { get; set; } = string.Empty;

    [Key(3)]
    public decimal Price { get; set; }

    [Key(4)]
    public int Stock { get; set; }

    [Key(5)]
    public DateTime CreatedAt { get; set; }

    [Key(6)]
    public List<string> Tags { get; set; } = [];

    [Key(7)]
    public Dictionary<string, string> Metadata { get; set; } = [];

    public static ProductModel CreateSample(int id) => new()
    {
        Id = id,
        Name = $"Product {id}",
        Description = "This is a sample product description used for serialization benchmarking.",
        Price = 99.99m + id,
        Stock = 100 + id,
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Tags = ["electronics", "sale", "featured", "new-arrival", "top-rated"],
        Metadata = new Dictionary<string, string>
        {
            ["brand"] = "BenchmarkBrand",
            ["color"] = "blue",
            ["size"] = "medium",
            ["origin"] = "Taiwan",
            ["warranty"] = "2years"
        }
    };
}
