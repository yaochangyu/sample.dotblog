using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Lab.API.Signature.Models;
using Lab.API.Signature.Repositories;
using Lab.API.Signature.Services;

namespace Lab.API.Signature.Tests.Services;

public class SignatureValidationHandlerTests
{
    private const string ApiKey = "demo-api-key-001";
    private const string Secret = "demo-secret-001-please-change";
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryApiKeyClientRepository : IApiKeyClientRepository
    {
        private readonly ApiKeyClient? _client;

        public InMemoryApiKeyClientRepository(ApiKeyClient? client) => _client = client;

        public Task<Maybe<ApiKeyClient>> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            var match = _client is not null && _client.ApiKey == apiKey ? _client : null;
            return Task.FromResult(Maybe<ApiKeyClient>.From(match));
        }
    }

    private static ApiKeyClient CreateDemoClient() => new()
    {
        ApiKey = ApiKey,
        Secret = Secret,
        ClientName = "Demo Partner One",
        CreatedAt = FixedNow
    };

    private static string ComputeSignature(string method, string path, string timestamp, string nonce, string apiKey, byte[] body, string secret)
    {
        var bodyHash = Convert.ToHexStringLower(SHA256.HashData(body));
        var canonicalString = string.Join("\n", method.ToUpperInvariant(), path, timestamp, nonce, apiKey, bodyHash);
        var hmac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonicalString));
        return Convert.ToHexStringLower(hmac);
    }

    private static SignatureValidationHandler CreateHandler(ApiKeyClient? client, DateTimeOffset now)
    {
        var repository = new InMemoryApiKeyClientRepository(client);
        return new SignatureValidationHandler(repository, new FixedTimeProvider(now));
    }

    [Fact]
    public async Task ValidateAsync_WithCorrectSignature_ReturnsSuccess()
    {
        var client = CreateDemoClient();
        var body = Encoding.UTF8.GetBytes("""{"amount":100}""");
        var timestamp = FixedNow.ToUnixTimeSeconds().ToString();
        const string nonce = "nonce-001";
        var signature = ComputeSignature("POST", "/api/protected/orders", timestamp, nonce, ApiKey, body, Secret);

        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "POST", "/api/protected/orders", QueryString: null, ApiKey, timestamp, nonce, signature, body);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(ApiKey, result.Value.ApiKey);
    }

    [Fact]
    public async Task ValidateAsync_WithTamperedBody_ReturnsSignatureMismatch()
    {
        var client = CreateDemoClient();
        var originalBody = Encoding.UTF8.GetBytes("""{"amount":100}""");
        var tamperedBody = Encoding.UTF8.GetBytes("""{"amount":999999}""");
        var timestamp = FixedNow.ToUnixTimeSeconds().ToString();
        const string nonce = "nonce-002";
        // 簽章是針對原始 body 計算，但實際送出的是被竄改後的 body
        var signature = ComputeSignature("POST", "/api/protected/orders", timestamp, nonce, ApiKey, originalBody, Secret);

        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "POST", "/api/protected/orders", QueryString: null, ApiKey, timestamp, nonce, signature, tamperedBody);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.SignatureMismatch, result.Error.Reason);
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredTimestamp_ReturnsTimestampExpired()
    {
        var client = CreateDemoClient();
        var body = Array.Empty<byte>();
        // Timestamp 落在 10 分鐘前，超出 ±5 分鐘視窗；簽章仍是針對此（過期的）timestamp 正確計算
        var expiredTime = FixedNow.AddMinutes(-10);
        var timestamp = expiredTime.ToUnixTimeSeconds().ToString();
        const string nonce = "nonce-003";
        var signature = ComputeSignature("GET", "/api/protected/orders/1", timestamp, nonce, ApiKey, body, Secret);

        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "GET", "/api/protected/orders/1", QueryString: null, ApiKey, timestamp, nonce, signature, body);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.TimestampExpired, result.Error.Reason);
    }

    [Fact]
    public async Task ValidateAsync_WithQueryString_ReturnsQueryStringNotAllowed()
    {
        var client = CreateDemoClient();
        var body = Array.Empty<byte>();
        var timestamp = FixedNow.ToUnixTimeSeconds().ToString();
        const string nonce = "nonce-004";
        var signature = ComputeSignature("GET", "/api/protected/orders/1", timestamp, nonce, ApiKey, body, Secret);

        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "GET", "/api/protected/orders/1", QueryString: "?foo=bar", ApiKey, timestamp, nonce, signature, body);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.QueryStringNotAllowed, result.Error.Reason);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingHeader_ReturnsHeaderMissing()
    {
        var client = CreateDemoClient();
        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "GET", "/api/protected/orders/1", QueryString: null, ApiKey,
            Timestamp: null, Nonce: "nonce-005", Signature: "abc", Array.Empty<byte>());

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.HeaderMissing, result.Error.Reason);
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownApiKey_ReturnsApiKeyNotFound()
    {
        var body = Array.Empty<byte>();
        var timestamp = FixedNow.ToUnixTimeSeconds().ToString();
        const string nonce = "nonce-006";
        var signature = ComputeSignature("GET", "/api/protected/orders/1", timestamp, nonce, "unknown-key", body, Secret);

        var handler = CreateHandler(client: null, FixedNow);
        var request = new SignatureValidationRequest(
            "GET", "/api/protected/orders/1", QueryString: null, "unknown-key", timestamp, nonce, signature, body);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.ApiKeyNotFound, result.Error.Reason);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(253402300800L)] // 超過 DateTimeOffset.MaxValue 支援範圍附近的邊界值
    public async Task ValidateAsync_WithOutOfRangeTimestamp_ReturnsTimestampInvalidFormatWithoutThrowing(long epochSeconds)
    {
        // Medium 1 修法驗證：超出 DateTimeOffset.FromUnixTimeSeconds 支援範圍的 long 不應該讓
        // ValidateAsync 丟出未處理例外（ArgumentOutOfRangeException），而是要回傳受控的 401 TimestampInvalidFormat。
        var client = CreateDemoClient();
        var body = Array.Empty<byte>();
        var timestamp = epochSeconds.ToString();
        const string nonce = "nonce-007";
        var signature = ComputeSignature("GET", "/api/protected/orders/1", timestamp, nonce, ApiKey, body, Secret);

        var handler = CreateHandler(client, FixedNow);
        var request = new SignatureValidationRequest(
            "GET", "/api/protected/orders/1", QueryString: null, ApiKey, timestamp, nonce, signature, body);

        var result = await handler.ValidateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.TimestampInvalidFormat, result.Error.Reason);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void ValidateCheapChecks_WithOutOfRangeTimestamp_ReturnsTimestampInvalidFormatWithoutThrowing(long epochSeconds)
    {
        // High 1 修法：Middleware 會在讀 body 前先呼叫 ValidateCheapChecks，這裡直接驗證
        // 這個「不需要 body」的方法本身對超出範圍的 timestamp 也不會丟例外。
        var handler = CreateHandler(CreateDemoClient(), FixedNow);

        var result = handler.ValidateCheapChecks(ApiKey, epochSeconds.ToString(), "nonce-008", "deadbeef", queryString: null);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.TimestampInvalidFormat, result.Error.Reason);
    }

    [Fact]
    public void ValidateCheapChecks_WithMissingHeader_ReturnsHeaderMissingWithoutTouchingRepository()
    {
        var handler = CreateHandler(client: null, FixedNow);

        var result = handler.ValidateCheapChecks(
            apiKey: null, timestamp: FixedNow.ToUnixTimeSeconds().ToString(), nonce: "n", signature: "s", queryString: null);

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.HeaderMissing, result.Error.Reason);
    }

    [Fact]
    public void ValidateCheapChecks_WithQueryString_ReturnsQueryStringNotAllowed()
    {
        var handler = CreateHandler(CreateDemoClient(), FixedNow);

        var result = handler.ValidateCheapChecks(
            ApiKey, FixedNow.ToUnixTimeSeconds().ToString(), "n", "s", queryString: "?foo=bar");

        Assert.True(result.IsFailure);
        Assert.Equal(SignatureValidationFailureReason.QueryStringNotAllowed, result.Error.Reason);
    }

    [Fact]
    public void SignatureComparison_UsesConstantTimeFixedTimeEquals()
    {
        // 依計畫要求，簽章比對必須用 CryptographicOperations.FixedTimeEquals，不可用 ==/string.Equals。
        // 以原始碼掃描確認 SignatureValidationHandler 內確實呼叫該 API（而不是靠計時側channel測試）。
        var sourcePath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Lab.API.Signature", "Services", "SignatureValidationHandler.cs");
        var sourceCode = File.ReadAllText(Path.GetFullPath(sourcePath));

        Assert.Contains("CryptographicOperations.FixedTimeEquals", sourceCode);
    }
}
