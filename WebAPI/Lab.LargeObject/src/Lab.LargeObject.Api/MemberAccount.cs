namespace Lab.LargeObject.Api;

public enum MemberStatus
{
    Active,
    Suspended,
    Deleted
}

public readonly struct ContactInfo
{
    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }
}

/// <summary>
/// 會員帳號——巢狀強型別物件（本身含 <see cref="ContactInfo"/>），
/// 刻意設計成 struct：陣列裡的每個元素直接內嵌在陣列的連續記憶體區塊裡，
/// 陣列本身才能被 <see cref="PooledMemberAccountArrayJsonConverter"/> 整包塞進 ArrayPool 租用的 buffer。
/// 如果改成 class，陣列裡存的只會是參考（指標），個別物件仍是各自獨立配置，ArrayPool 就幫不上忙。
/// </summary>
public readonly struct MemberAccount
{
    public required long MemberId { get; init; }

    public required string Account { get; init; }

    public required string DisplayName { get; init; }

    public required MemberStatus Status { get; init; }

    public required ContactInfo Contact { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
