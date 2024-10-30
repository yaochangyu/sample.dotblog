using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.Sharding.Infrastructure;

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(
            reader.GetString(),
            DateTimeExtensions.DefaultDateTimeCultureInfo,
            DateTimeStyles.AdjustToUniversal);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset dateTimeValue,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(dateTimeValue.ToUtcString());
    }
}