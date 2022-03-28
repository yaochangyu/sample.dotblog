using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.ORM.DynamicField;

public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader,
                                                    Type typeToConvert,
                                                    JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var results = new Dictionary<string, object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return results;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();

            results.Add(propertyName, this.ReadValue(ref reader, options));
        }

        return results;
    }

    public override void Write(Utf8JsonWriter writer,
                               Dictionary<string, object> value,
                               JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var key in value.Keys)
        {
            WriteValue(writer, key, value[key], options);
        }

        writer.WriteEndObject();
    }

    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTimeOffset(out var dateOffset))
                {
                    return dateOffset;
                }

                if (reader.TryGetGuid(out var guid))
                {
                    return guid;
                }

                return reader.GetString();
            case JsonTokenType.False:
            case JsonTokenType.True:
                return reader.GetBoolean();
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var result))
                {
                    return result;
                }

                return reader.GetDecimal();
            case JsonTokenType.StartObject:
                return this.Read(ref reader, null, options);
            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(this.ReadValue(ref reader, options));
                }

                return list;
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }

    private static void WriteValue(Utf8JsonWriter writer,
                                   string key,
                                   object value,
                                   JsonSerializerOptions options)
    {
        if (key != null)
        {
            writer.WritePropertyName(key);
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}