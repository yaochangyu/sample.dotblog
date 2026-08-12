using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.LargeObject.Api;

/// <summary>
/// 把 JSON 數字陣列反序列化進一塊從 <see cref="ArrayPool{T}"/> 租來的 buffer，
/// 而不是讓 System.Text.Json 用 <c>new double[]</c> 直接配置一塊大物件（LOH）。
/// buffer 不夠大時用倍增策略換更大的租用陣列，並歸還舊的。
/// </summary>
public sealed class PooledDoubleArrayJsonConverter : JsonConverter<PooledArray<double>>
{
    private const int InitialCapacity = 4096;

    public override PooledArray<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of a JSON array of numbers.");
        }

        var buffer = ArrayPool<double>.Shared.Rent(InitialCapacity);
        var count = 0;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == buffer.Length)
                {
                    var larger = ArrayPool<double>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, larger, count);
                    ArrayPool<double>.Shared.Return(buffer);
                    buffer = larger;
                }

                buffer[count++] = reader.GetDouble();
            }

            return new PooledArray<double>(buffer, count);
        }
        catch
        {
            ArrayPool<double>.Shared.Return(buffer);
            throw;
        }
    }

    public override void Write(Utf8JsonWriter writer, PooledArray<double> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Span)
        {
            writer.WriteNumberValue(item);
        }
        writer.WriteEndArray();
    }
}
