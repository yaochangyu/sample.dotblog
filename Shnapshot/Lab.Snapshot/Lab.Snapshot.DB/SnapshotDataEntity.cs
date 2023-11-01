using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lab.Snapshot.DB;

public class SnapshotDataEntity
{
    public string Id { get; set; }

    public string DataType { get; set; }

    [Column(TypeName = "jsonb")]
    public JsonNode Data { get; set; }

    public string DataFormat { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public int Version { get; set; }
}