using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lab.JsonConverterLib;

public static class JsonNodeExtensions
{
    public static T To<T>(this JsonNode source,
                          JsonSerializerOptions options = default)
    {
        return source.Deserialize<T>(options);
    }

    public static JsonNode ToJsonNode<T>(this T source,
                                         JsonNodeOptions options = default)
        where T : class
    {
        return JsonNode.Parse(JsonSerializer.SerializeToUtf8Bytes(source), options);
    }

    public static JsonNode ToJsonNode(this string source,
                                      JsonNodeOptions options = default)
    {
        return JsonNode.Parse(source, options);
    }

    public static string ToJsonString(this JsonNode source,
                                      JsonWriterOptions options = default)
    {
        if (source == null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);
        source.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}