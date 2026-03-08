using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdempotencyKey.WebApi;

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
