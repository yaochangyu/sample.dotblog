using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdempotencyKey.WebApi;

public static class JsonSerializeFactory
{
    private static readonly Lazy<JsonSerializerOptions> s_defaultOptionLazy = new(CreateDefaultOptions);

    public static JsonSerializerOptions DefaultOptions => s_defaultOptionLazy.Value;

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions();
        Apply(options);
        return options;
    }

    public static void Apply(JsonSerializerOptions options)
    {
        options.MaxDepth = 20;
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DateTimeOffsetJsonConverter());
    }
}

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return raw is null
            ? default
            : DateTimeOffset.Parse(raw, DateTimeExtensions.DefaultDateTimeCultureInfo);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset dateTimeValue,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(dateTimeValue.ToUtcString());
    }
}

public static class DateTimeExtensions
{
    public const string DefaultDateTimeFormat = "o";
    public static readonly CultureInfo DefaultDateTimeCultureInfo = CultureInfo.InvariantCulture;

    public static IEnumerable<(DateTime start, DateTime end)> Each(this DateTime inputStart, DateTime inputEnd,
        TimeSpan step)
    {
        DateTime dtStart, dtEnd;
        dtStart = inputStart;
        while (dtStart < inputEnd)
        {
            dtEnd = dtStart + step;
            if (dtEnd > inputEnd)
            {
                dtEnd = inputEnd;
            }

            yield return (dtStart, dtEnd);

            dtStart += step;
        }
    }

    public static string ToUtcString(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.ToString(DefaultDateTimeFormat, DefaultDateTimeCultureInfo);
    }

    public static string ToUtcString(this DateTime dt)
    {
        return dt.ToString(DefaultDateTimeFormat, DefaultDateTimeCultureInfo);
    }
}