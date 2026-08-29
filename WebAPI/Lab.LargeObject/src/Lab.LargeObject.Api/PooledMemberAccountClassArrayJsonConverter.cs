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

                buffer[count++] = ReadMemberAccountClass(ref reader);
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

    private static MemberAccountClass ReadMemberAccountClass(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of a member account JSON object.");
        }

        long? memberId = null;
        string? account = null;
        string? displayName = null;
        MemberStatus? status = null;
        ContactInfoClass? contact = null;
        DateTimeOffset? createdAt = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
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
                status = reader.TokenType == JsonTokenType.Number
                    ? (MemberStatus)reader.GetInt32()
                    : Enum.Parse<MemberStatus>(reader.GetString()!, ignoreCase: true);
            }
            else if (reader.ValueTextEquals("contact"u8))
            {
                reader.Read();
                contact = ReadContactInfoClass(ref reader);
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

        return new MemberAccountClass
        {
            MemberId = memberId ?? throw new JsonException("memberId is required"),
            Account = account ?? throw new JsonException("account is required"),
            DisplayName = displayName ?? throw new JsonException("displayName is required"),
            Status = status ?? throw new JsonException("status is required"),
            Contact = contact ?? throw new JsonException("contact is required"),
            CreatedAt = createdAt ?? throw new JsonException("createdAt is required")
        };
    }

    private static ContactInfoClass ReadContactInfoClass(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of contact info JSON object.");
        }

        string? email = null;
        string? phone = null;

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
                phone = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        return new ContactInfoClass
        {
            Email = email ?? throw new JsonException("email is required"),
            PhoneNumber = phone
        };
    }
}
