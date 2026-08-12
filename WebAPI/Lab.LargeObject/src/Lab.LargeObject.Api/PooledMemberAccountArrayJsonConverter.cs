using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.LargeObject.Api;

/// <summary>
/// 把 JSON 陣列（元素是巢狀的 <see cref="MemberAccount"/> 物件）反序列化進一塊從
/// <see cref="ArrayPool{T}"/> 租來的 buffer，取代 System.Text.Json 預設的 <c>new MemberAccount[]</c>。
/// 只有「陣列容器」走 ArrayPool；每個元素內部的巢狀物件（<see cref="ContactInfo"/> 等）
/// 一樣交給 System.Text.Json 用標準方式遞迴反序列化——ArrayPool 解決的是陣列本身的大小，
/// 不是也不需要處理個別小物件的配置。
/// </summary>
public sealed class PooledMemberAccountArrayJsonConverter : JsonConverter<PooledArray<MemberAccount>>
{
    private const int InitialCapacity = 1024;

    public override PooledArray<MemberAccount> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of a JSON array of member accounts.");
        }

        var buffer = ArrayPool<MemberAccount>.Shared.Rent(InitialCapacity);
        var count = 0;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == buffer.Length)
                {
                    var larger = ArrayPool<MemberAccount>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, larger, count);
                    ArrayPool<MemberAccount>.Shared.Return(buffer, clearArray: true);
                    buffer = larger;
                }

                // reader 目前指向這個陣列元素的 StartObject；巢狀欄位（含 ContactInfo）
                // 交給 System.Text.Json 用標準反序列化遞迴處理，讀完後 reader 會停在該元素的 EndObject。
                buffer[count++] = JsonSerializer.Deserialize<MemberAccount>(ref reader, options);
            }

            return new PooledArray<MemberAccount>(buffer, count);
        }
        catch
        {
            ArrayPool<MemberAccount>.Shared.Return(buffer, clearArray: true);
            throw;
        }
    }

    public override void Write(Utf8JsonWriter writer, PooledArray<MemberAccount> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Span)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
        writer.WriteEndArray();
    }
}
