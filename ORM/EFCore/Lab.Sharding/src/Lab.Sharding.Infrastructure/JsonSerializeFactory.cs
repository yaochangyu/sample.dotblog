using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.Sharding.Infrastructure;

public class JsonSerializeFactory
{
    private static readonly Lazy<JsonSerializerOptions> s_defaultOptionLazy = new(CreateDefaultOptions);

    public static JsonSerializerOptions DefaultOptions => s_defaultOptionLazy.Value;

    public static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions();
        Apply(options);
        return options;
    }

    public static JsonSerializerOptions Apply(JsonSerializerOptions options)
    {
        options.MaxDepth = 20;
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DateTimeOffsetJsonConverter());
        return options;
    }
}