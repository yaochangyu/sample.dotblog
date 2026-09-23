namespace Lab.Signature.WebApi.Models;

/// <summary>
/// 代表一組可呼叫簽章保護 API 的合作夥伴憑證。
/// ⚠️ Secret 為明碼存放（HMAC 驗證需要原始值重算比對），僅供教學使用，
/// 正式環境需搭配 Secret Manager/KMS，詳見 README「Secret 風險揭露清單」。
/// </summary>
public class ApiKeyClient
{
    public string ApiKey { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
