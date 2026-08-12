using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.LargeObject.Api;

/// <summary>
/// 把 JSON 陣列（元素是巢狀的 <see cref="MemberAccount"/> 物件）反序列化進一塊從
/// <see cref="ArrayPool{T}"/> 租來的 buffer，取代 System.Text.Json 預設的 <c>new MemberAccount[]</c>。
/// </summary>
/// <remarks>
/// 巢狀欄位（含 <see cref="ContactInfo"/>）刻意<b>不</b>透過 <c>JsonSerializer.Deserialize&lt;T&gt;(ref reader, options)</c>
/// 遞迴處理——實測（見 repo 的 <c>.issues/deserialize-allocation-reduction.issues.md</c>）發現
/// 每個陣列元素各自呼叫一次頂層 <c>Deserialize&lt;T&gt;</c>，會反覆重建 read stack／argument state 追蹤，
/// 不管背後是反射還是 Source Generator 都一樣，是主要的配置量來源。改成跟 <see cref="PooledDoubleArrayJsonConverter"/>
/// 一致的手法：直接用 <see cref="Utf8JsonReader"/> 逐欄位手刻解析，完全繞開 System.Text.Json 的 metadata／
/// argument-state 機制。
/// </remarks>
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

                buffer[count++] = ReadMemberAccount(ref reader);
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

    private static MemberAccount ReadMemberAccount(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of a member account JSON object.");
        }

        long? memberId = null;
        string? account = null;
        string? displayName = null;
        MemberStatus? status = null;
        ContactInfo? contact = null;
        DateTimeOffset? createdAt = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            // 用 ValueTextEquals 在 UTF8 位元組層級比對屬性名稱，不呼叫 GetString()——
            // 避免每個屬性名稱都額外配置一個字串（這是手刻解析第一版量到 System.String 配置量不降反升的原因）。
            if (reader.ValueTextEquals("memberId"u8))
            {
                reader.Read();
                memberId = reader.GetInt64();
            }
            else if (reader.ValueTextEquals("account"u8))
            {
                reader.Read();
                account = reader.GetString();
            }
            else if (reader.ValueTextEquals("displayName"u8))
            {
                reader.Read();
                displayName = reader.GetString();
            }
            else if (reader.ValueTextEquals("status"u8))
            {
                reader.Read();
                // 跟 JsonStringEnumConverter 的行為對齊：字串（"Active"）或數字（0）都接受。
                status = reader.TokenType == JsonTokenType.Number
                    ? (MemberStatus)reader.GetInt32()
                    : Enum.Parse<MemberStatus>(reader.GetString()!, ignoreCase: true);
            }
            else if (reader.ValueTextEquals("contact"u8))
            {
                reader.Read();
                contact = ReadContactInfo(ref reader);
            }
            else if (reader.ValueTextEquals("createdAt"u8))
            {
                reader.Read();
                createdAt = reader.GetDateTimeOffset();
            }
            else
            {
                reader.Skip();
            }
        }

        if (memberId is null || account is null || displayName is null || status is null || contact is null || createdAt is null)
        {
            throw new JsonException("Member account JSON object is missing one or more required properties.");
        }

        return new MemberAccount
        {
            MemberId = memberId.Value,
            Account = account,
            DisplayName = displayName,
            Status = status.Value,
            Contact = contact.Value,
            CreatedAt = createdAt.Value
        };
    }

    private static ContactInfo ReadContactInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of a contact info JSON object.");
        }

        string? email = null;
        string? phoneNumber = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("email"u8))
            {
                reader.Read();
                email = reader.GetString();
            }
            else if (reader.ValueTextEquals("phoneNumber"u8))
            {
                reader.Read();
                phoneNumber = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        if (email is null)
        {
            throw new JsonException("Contact info JSON object is missing required property 'email'.");
        }

        return new ContactInfo { Email = email, PhoneNumber = phoneNumber };
    }
}
