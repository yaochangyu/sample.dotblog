using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Lab.Signature.WebApi.Models;
using Lab.Signature.WebApi.Repositories;

namespace Lab.Signature.WebApi.Services;

/// <summary>
/// 依「Canonical String 規格」組裝簽章原文並用 HMAC-SHA256 驗證。
/// 驗證順序：Header 齊全 → 不可帶 Query String → Timestamp 格式/範圍與 ±5 分鐘視窗（<see cref="ValidateCheapChecks"/>，
/// 不需要 body） → ApiKey 存在 → 簽章比對（<see cref="ValidateAsync"/>，需要 body）。
/// 尚未包含 Nonce 防重放檢查（Step 6 才會整合到 Middleware）。
/// </summary>
public class SignatureValidationHandler : ISignatureValidationHandler
{
    private static readonly TimeSpan TimestampWindow = TimeSpan.FromMinutes(5);

    // DateTimeOffset.FromUnixTimeSeconds 只接受這個範圍內的值，超出範圍會丟 ArgumentOutOfRangeException。
    // 先用這兩個邊界值做範圍檢查，避免把例外丟到呼叫端變成未受控的 500。
    private static readonly long MinEpochSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();
    private static readonly long MaxEpochSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    private readonly IApiKeyClientRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignatureValidationHandler(IApiKeyClientRepository repository, TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public UnitResult<SignatureValidationFailure> ValidateCheapChecks(
        string? apiKey,
        string? timestamp,
        string? nonce,
        string? signature,
        string? queryString) =>
        ValidateHeadersQueryStringAndTimestamp(apiKey, timestamp, nonce, signature, queryString, _timeProvider);

    public async Task<Result<ApiKeyClient, SignatureValidationFailure>> ValidateAsync(
        SignatureValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var cheapCheckResult = ValidateHeadersQueryStringAndTimestamp(
            request.ApiKey, request.Timestamp, request.Nonce, request.Signature, request.QueryString, _timeProvider);
        if (cheapCheckResult.IsFailure)
        {
            return Result.Failure<ApiKeyClient, SignatureValidationFailure>(cheapCheckResult.Error);
        }

        var clientMaybe = await _repository.FindByApiKeyAsync(request.ApiKey!, cancellationToken);
        if (clientMaybe.HasNoValue)
        {
            return Result.Failure<ApiKeyClient, SignatureValidationFailure>(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.ApiKeyNotFound,
                    "X-Api-Key 不存在"));
        }

        var client = clientMaybe.Value;
        var canonicalString = BuildCanonicalString(
            request.Method,
            request.Path,
            request.Timestamp!,
            request.Nonce!,
            request.ApiKey!,
            request.Body);
        var expectedSignatureHex = ComputeHmacSha256Hex(canonicalString, client.Secret);

        if (!IsSignatureMatch(expectedSignatureHex, request.Signature!))
        {
            return Result.Failure<ApiKeyClient, SignatureValidationFailure>(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.SignatureMismatch,
                    "簽章不符"));
        }

        return Result.Success<ApiKeyClient, SignatureValidationFailure>(client);
    }

    /// <summary>
    /// 「便宜檢查」：Header 齊全 → QueryString → Timestamp 格式/範圍/±5 分鐘視窗。
    /// 不需要讀取或依賴 request body，也不查詢資料庫，讓 Middleware 能在完整讀 body 之前先過濾掉
    /// 大多數格式錯誤/缺 Header/過期的請求，避免匿名攻擊者用大 body 消耗記憶體與 CPU。
    /// </summary>
    private static UnitResult<SignatureValidationFailure> ValidateHeadersQueryStringAndTimestamp(
        string? apiKey,
        string? timestamp,
        string? nonce,
        string? signature,
        string? queryString,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrEmpty(apiKey)
            || string.IsNullOrEmpty(timestamp)
            || string.IsNullOrEmpty(nonce)
            || string.IsNullOrEmpty(signature))
        {
            return UnitResult.Failure(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.HeaderMissing,
                    "缺少 X-Api-Key / X-Timestamp / X-Nonce / X-Signature 其中一個必要 Header"));
        }

        if (!string.IsNullOrEmpty(queryString))
        {
            return UnitResult.Failure(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.QueryStringNotAllowed,
                    "Protected 端點不支援 Query String"));
        }

        if (!long.TryParse(timestamp, out var epochSeconds))
        {
            return UnitResult.Failure(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.TimestampInvalidFormat,
                    "X-Timestamp 必須是 Unix epoch seconds 數字字串"));
        }

        // epochSeconds 若超出 DateTimeOffset 支援的範圍，FromUnixTimeSeconds 會丟 ArgumentOutOfRangeException，
        // 這裡先用範圍檢查擋掉，讓超出範圍的值一律視為格式錯誤（401），而不是變成未受控的例外（500）。
        if (epochSeconds < MinEpochSeconds || epochSeconds > MaxEpochSeconds)
        {
            return UnitResult.Failure(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.TimestampInvalidFormat,
                    "X-Timestamp 超出合理的 Unix epoch seconds 範圍"));
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        var now = timeProvider.GetUtcNow();
        if ((now - requestTime).Duration() > TimestampWindow)
        {
            return UnitResult.Failure(
                new SignatureValidationFailure(
                    SignatureValidationFailureReason.TimestampExpired,
                    "X-Timestamp 超出允許的 ±5 分鐘視窗"));
        }

        return UnitResult.Success<SignatureValidationFailure>();
    }

    /// <summary>
    /// 依規格組裝 Canonical String：METHOD\nPATH\nTIMESTAMP\nNONCE\nAPI_KEY\nBODY_SHA256_HEX。
    /// </summary>
    internal static string BuildCanonicalString(
        string method,
        string path,
        string timestamp,
        string nonce,
        string apiKey,
        byte[] body)
    {
        var bodyHashHex = ComputeSha256Hex(body);
        return string.Join(
            "\n",
            method.ToUpperInvariant(),
            path,
            timestamp,
            nonce,
            apiKey,
            bodyHashHex);
    }

    /// <summary>對 raw bytes 計算 SHA-256，輸出小寫 hex；空 body 會得到空字串的 SHA-256 值。</summary>
    internal static string ComputeSha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>對 Canonical String 計算 HMAC-SHA256，輸出小寫 hex。</summary>
    internal static string ComputeHmacSha256Hex(string canonicalString, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(canonicalString);
        var hash = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// 用 constant-time 比對簽章。非合法 hex 或長度不符視為不相符，
    /// 內容比對一律走 <see cref="CryptographicOperations.FixedTimeEquals"/>，禁止用 == 或 string.Equals。
    /// </summary>
    private static bool IsSignatureMatch(string expectedSignatureHex, string providedSignature)
    {
        byte[] providedBytes;
        try
        {
            providedBytes = Convert.FromHexString(providedSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var expectedBytes = Convert.FromHexString(expectedSignatureHex);

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
