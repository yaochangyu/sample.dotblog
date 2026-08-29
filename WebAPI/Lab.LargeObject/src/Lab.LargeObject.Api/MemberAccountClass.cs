namespace Lab.LargeObject.Api;

public class ContactInfoClass
{
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// 會員帳號（Class 參考型別版本）
/// 與 <see cref="MemberAccount"/> (struct) 作為對照組。
/// 當宣告為 class 時，陣列內只存放 8 bytes 的記憶體位址指標，
/// 每個元素本身依然會各自在 Heap (Gen0) 配置獨立實體。
/// </summary>
public class MemberAccountClass
{
    public required long MemberId { get; init; }
    public required string Account { get; init; }
    public required string DisplayName { get; init; }
    public required MemberStatus Status { get; init; }
    public required ContactInfoClass Contact { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
