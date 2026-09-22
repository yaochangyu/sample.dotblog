using System.Security.Cryptography;
using System.Text;

namespace Lab.API.Signature.Tests.Support;

/// <summary>
/// 測試專用的簽章計算 helper，故意跟 production code
/// （<see cref="Lab.API.Signature.Services.SignatureValidationHandler"/>）分開獨立實作，
/// 避免測試因為直接複用 internal production 方法而變成「測自己」。
/// 邏輯必須跟 Canonical String 規格一致：
/// METHOD(大寫)\nPATH\nTIMESTAMP\nNONCE\nAPI_KEY\nBODY_SHA256_HEX。
/// </summary>
public static class SignatureTestHelper
{
    public static string ComputeSha256Hex(string bodyText)
    {
        var bytes = Encoding.UTF8.GetBytes(bodyText ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public static string BuildCanonicalString(
        string method,
        string path,
        string timestamp,
        string nonce,
        string apiKey,
        string bodyHashHex)
    {
        return string.Join("\n", method.ToUpperInvariant(), path, timestamp, nonce, apiKey, bodyHashHex);
    }

    public static string ComputeHmacSha256Hex(string canonicalString, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(canonicalString);
        var hash = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexStringLower(hash);
    }
}
