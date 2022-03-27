using System.Text;
using System.Text.Json;

namespace Lab.JsonConverterLib;

public static class JsonDocumentExtensions
{
    public static T To<T>(this JsonDocument source,
                          JsonSerializerOptions options = default)
    {
        return source.Deserialize<T>(options);
    }

    public static JsonDocument ToJsonDocument<T>(this T source,
                                                 JsonDocumentOptions options = default)
        where T : class
    {
        return JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(source), options);
    }

    public static JsonDocument ToJsonDocument(this string source,
                                              JsonDocumentOptions options = default)
    {
        return JsonDocument.Parse(source, options);
    }

    public static string ToJsonString(this JsonDocument source,
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