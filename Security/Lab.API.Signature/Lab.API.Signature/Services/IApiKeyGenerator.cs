namespace Lab.API.Signature.Services;

/// <summary>
/// 產生新核發的 ApiKey / Secret。使用 <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
/// 產生密碼學安全的隨機值，無狀態、可安全共用單一實例。
/// </summary>
public interface IApiKeyGenerator
{
    /// <summary>產生格式為 <c>key-</c> + 32 碼小寫 hex（16 random bytes）的 ApiKey。</summary>
    string GenerateApiKey();

    /// <summary>產生 64 碼小寫 hex（32 random bytes）的 Secret，僅在核發當下顯示一次。</summary>
    string GenerateSecret();
}
