using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.LargeObject.Api;

/// <summary>
/// 針對 string[] 的 ArrayPool JsonConverter
/// </summary>
public sealed class PooledStringArrayJsonConverter : JsonConverter<PooledArray<string>>
{
    private const int InitialCapacity = 16;

    public override PooledArray<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"預期 StartArray，實際拿到 {reader.TokenType}");
        }

        var rented = ArrayPool<string>.Shared.Rent(InitialCapacity);
        var count = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return new PooledArray<string>(rented, count);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                if (count == rented.Length)
                {
                    var newRented = ArrayPool<string>.Shared.Rent(rented.Length * 2);
                    Array.Copy(rented, newRented, count);
                    ArrayPool<string>.Shared.Return(rented, clearArray: true);
                    rented = newRented;
                }

                rented[count++] = reader.GetString()!;
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                if (count == rented.Length)
                {
                    var newRented = ArrayPool<string>.Shared.Rent(rented.Length * 2);
                    Array.Copy(rented, newRented, count);
                    ArrayPool<string>.Shared.Return(rented, clearArray: true);
                    rented = newRented;
                }

                rented[count++] = null!;
            }
            else
            {
                throw new JsonException($"預期 String，實際拿到 {reader.TokenType}");
            }
        }

        throw new JsonException("JSON 串流在陣列結束前意外中斷");
    }

    public override void Write(
        Utf8JsonWriter writer,
        PooledArray<string> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var span = value.Span;
        for (var i = 0; i < span.Length; i++)
        {
            writer.WriteStringValue(span[i]);
        }
        writer.WriteEndArray();
    }
}
