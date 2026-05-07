using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Services;

public class PkceService
{
    /// <summary>
    /// 驗證 SHA256(codeVerifier) 的 Base64Url 編碼是否等於 codeChallenge
    /// </summary>
    public bool Verify(string codeVerifier, string codeChallenge)
    {
        var computed = ComputeChallenge(codeVerifier);
        // 使用固定時間比較，避免 timing attack
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(codeChallenge)
        );
    }

    private static string ComputeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
}
