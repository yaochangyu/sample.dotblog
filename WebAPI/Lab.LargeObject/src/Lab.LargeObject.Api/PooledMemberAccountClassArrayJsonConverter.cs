using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.LargeObject.Api;

/// <summary>
/// 針對 Class 版本的 MemberAccount 進行 ArrayPool 租用。
/// 實測對照：池化只能省下 MemberAccountClass[] 指標陣列本身，
/// 但每個元素依舊必須 new MemberAccountClass 與 new ContactInfoClass，
/// 無法像 struct 一樣將資料內嵌在連續 Buffer 中。
/// </summary>
public sealed class PooledMemberAccountClassArrayJsonConverter : JsonConverter<PooledArray<MemberAccountClass>>
{
    private const int InitialCapacity = 1024;

    public override PooledArray<MemberAccountClass> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of a JSON array of member accounts.");
        }

        var buffer = ArrayPool<MemberAccountClass>.Shared.Rent(InitialCapacity);
        var count = 0;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == buffer.Length)
                {
                    var larger = ArrayPool<MemberAccountClass>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, larger, count);
                    ArrayPool<MemberAccountClass>.Shared.Return(buffer, clearArray: true);
                    buffer = larger;
                }

                buffer[count++] = JsonSerializer.Deserialize<MemberAccountClass>(ref reader, options)
                    ?? throw new JsonException("Member account element cannot be null.");
            }

            return new PooledArray<MemberAccountClass>(buffer, count);
        }
        catch
        {
            ArrayPool<MemberAccountClass>.Shared.Return(buffer, clearArray: true);
            throw;
        }
    }

    public override void Write(Utf8JsonWriter writer, PooledArray<MemberAccountClass> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Span)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
        writer.WriteEndArray();
    }
}

