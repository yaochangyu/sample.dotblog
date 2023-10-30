using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Lab.Snapshot.DB;

public class SnapshotDataEntity
{
    public string Id { get; set; }

    public string Type { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Data { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public int Version { get; set; }
}