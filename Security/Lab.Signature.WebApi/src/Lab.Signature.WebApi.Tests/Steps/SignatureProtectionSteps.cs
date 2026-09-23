using System.Net;
using System.Text;
using Lab.Signature.WebApi.Data;
using Lab.Signature.WebApi.Models;
using Lab.Signature.WebApi.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace Lab.Signature.WebApi.Tests.Steps;

[Binding]
public class SignatureProtectionSteps
{
    private readonly SignatureScenarioContext _context;

    public SignatureProtectionSteps(SignatureScenarioContext context)
    {
        _context = context;
    }

    // ---------- Given ----------

    [Given(@"我已經在系統中註冊了一個新的 ApiKey")]
    public async Task GivenIHaveRegisteredANewApiKey()
    {
        (_context.ApiKey, _context.Secret) = await RegisterNewApiKeyClientAsync();
    }

    [Given(@"我已經在系統中額外註冊了另一個 ApiKey")]
    public async Task GivenIHaveRegisteredAnotherApiKey()
    {
        (_context.SecondaryApiKey, _context.SecondarySecret) = await RegisterNewApiKeyClientAsync();
    }

    [Given(@"我使用一個從未註冊過的 ApiKey")]
    public void GivenIUseAnUnregisteredApiKey()
    {
        _context.ApiKey = $"bdd-unregistered-{Guid.NewGuid():N}";
        _context.Secret = "bdd-unused-secret-because-apikey-not-found";
    }

    [Given(@"Body 是:")]
    public void GivenBodyIs(string bodyText)
    {
        _context.BodyText = bodyText;
    }

    [Given(@"我在送出前把 Body 竄改成:")]
    public void GivenITamperBodyTo(string tamperedBodyText)
    {
        _context.TamperedBodyText = tamperedBodyText;
    }

    [Given(@"我沒有帶 ""(.*)"" 這個 Header")]
    public void GivenIOmitHeader(string headerName)
    {
        _context.HeadersToOmit.Add(headerName);
    }

    [Given(@"QueryString 帶有 ""(.*)""")]
    public void GivenQueryStringIs(string queryString)
    {
        _context.QueryString = queryString;
    }

    [Given(@"Signature 故意轉成大寫")]
    public void GivenSignatureUppercase()
    {
        _context.SignatureTransform = s => s.ToUpperInvariant();
    }

    [Given(@"Signature 故意換成不合法的非 hex 字串")]
    public void GivenSignatureInvalidHex()
    {
        _context.SignatureOverride = "zz-not-a-valid-hex-signature-zz";
    }

    [Given(@"Nonce 固定為 ""(.*)""")]
    public void GivenNonceFixed(string nonce)
    {
        _context.NonceOverride = nonce;
    }

    [Given(@"Timestamp 是 (\d+) 分鐘(前|後)")]
    public void GivenTimestampIsMinutesOffset(int minutes, string direction)
    {
        var offsetMinutes = direction == "前" ? -minutes : minutes;
        _context.TimestampOverride = DateTimeOffset.UtcNow.AddMinutes(offsetMinutes).ToUnixTimeSeconds().ToString();
    }

    [Given(@"Timestamp 是格式錯誤的字串 ""(.*)""")]
    public void GivenTimestampIsInvalidFormat(string value)
    {
        _context.TimestampOverride = value;
    }

    // ---------- When ----------

    [When(@"^我送出 GET /api/protected/orders/\{id\} 請求$")]
    public Task WhenISendGetRequest() => SendRequestAsync("GET", "/api/protected/orders/1");

    [When(@"^我送出 POST /api/protected/orders 請求$")]
    public Task WhenISendPostRequest() => SendRequestAsync("POST", "/api/protected/orders");

    [When(@"我沿用上一次的 Timestamp 與 Nonce 重新送出相同請求")]
    public Task WhenIResendSameRequest() => SendRequestAsync(
        _context.LastMethod!,
        _context.LastBasePath!,
        reuseLastNonceTimestampAndSignature: true);

    [When(@"^我使用第一組 ApiKey 送出 GET /api/protected/orders/\{id\} 請求$")]
    public Task WhenIUseFirstApiKeyToSendGetRequest() => SendRequestAsync(
        "GET",
        "/api/protected/orders/1",
        apiKeyOverride: _context.ApiKey,
        secretOverride: _context.Secret);

    [When(@"^我使用第二組 ApiKey 送出 GET /api/protected/orders/\{id\} 請求$")]
    public Task WhenIUseSecondApiKeyToSendGetRequest() => SendRequestAsync(
        "GET",
        "/api/protected/orders/1",
        apiKeyOverride: _context.SecondaryApiKey,
        secretOverride: _context.SecondarySecret);

    [When(@"我同時送出 (\d+) 個使用相同 Nonce 的並發請求")]
    public async Task WhenIConcurrentlySendRequestsWithSameNonce(int count)
    {
        const string method = "GET";
        const string basePath = "/api/protected/orders/1";

        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var bodyHashHex = SignatureTestHelper.ComputeSha256Hex(string.Empty);
        var canonicalString = SignatureTestHelper.BuildCanonicalString(
            method, basePath, timestamp, nonce, _context.ApiKey, bodyHashHex);
        var signature = SignatureTestHelper.ComputeHmacSha256Hex(canonicalString, _context.Secret);

        var client = TestRunHooks.Factory.CreateClient();

        var tasks = Enumerable.Range(0, count).Select(async _ =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, basePath);
            request.Headers.Add("X-Api-Key", _context.ApiKey);
            request.Headers.Add("X-Timestamp", timestamp);
            request.Headers.Add("X-Nonce", nonce);
            request.Headers.Add("X-Signature", signature);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            return new ConcurrentCallResult(response.StatusCode, body);
        });

        _context.ConcurrentResults = await Task.WhenAll(tasks);
    }

    // ---------- Then ----------

    [Then(@"回應狀態碼應該是 (\d+)")]
    public void ThenStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, (int)_context.LastResponse!.StatusCode);
    }

    [Then(@"回應內容應該包含 reason ""(.*)""")]
    public void ThenResponseShouldContainReason(string reason)
    {
        Assert.Contains($"\"reason\":\"{reason}\"", _context.LastResponseBody);
    }

    [Then(@"回應內容不應該包含 Secret 字串")]
    public void ThenResponseShouldNotContainSecret()
    {
        Assert.DoesNotContain(_context.Secret, _context.LastResponseBody);
    }

    [Then(@"回應內容不應該包含任何 HMAC 中間計算值")]
    public void ThenResponseShouldNotContainHmacIntermediateValues()
    {
        Assert.DoesNotContain(_context.LastSignature ?? "__no_signature__", _context.LastResponseBody);
        Assert.DoesNotContain(_context.LastCanonicalString ?? "__no_canonical__", _context.LastResponseBody);
    }

    [Then(@"回應內容應該包含 productName ""(.*)"" 與 amount (\d+)")]
    public void ThenResponseShouldContainOrderDetails(string productName, int amount)
    {
        Assert.Contains(productName, _context.LastResponseBody);
        Assert.Contains(amount.ToString(), _context.LastResponseBody);
    }

    [Then(@"剛好有 (\d+) 個請求成功、其餘 (\d+) 個請求都回傳 401 reason ""(.*)""")]
    public void ThenExactlyNSucceedRestFailWithReason(int expectedSuccessCount, int expectedFailCount, string reason)
    {
        var results = _context.ConcurrentResults!;
        var successResults = results.Where(r => r.StatusCode == HttpStatusCode.OK).ToArray();
        var failedResults = results.Where(r => r.StatusCode == HttpStatusCode.Unauthorized).ToArray();

        Assert.Equal(expectedSuccessCount, successResults.Length);
        Assert.Equal(expectedFailCount, failedResults.Length);
        Assert.All(failedResults, r => Assert.Contains($"\"reason\":\"{reason}\"", r.Body));
    }

    // ---------- Helpers ----------

    private static async Task<(string ApiKey, string Secret)> RegisterNewApiKeyClientAsync()
    {
        var apiKey = $"bdd-{Guid.NewGuid():N}";
        var secret = $"bdd-secret-{Guid.NewGuid():N}";

        using var scope = TestRunHooks.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SignatureDbContext>();
        dbContext.ApiKeyClients.Add(new ApiKeyClient
        {
            ApiKey = apiKey,
            Secret = secret,
            ClientName = "BDD Test Client",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        return (apiKey, secret);
    }

    private async Task SendRequestAsync(
        string method,
        string basePath,
        bool reuseLastNonceTimestampAndSignature = false,
        string? apiKeyOverride = null,
        string? secretOverride = null)
    {
        var apiKey = apiKeyOverride ?? _context.ApiKey;
        var secret = secretOverride ?? _context.Secret;

        var bodyToSign = _context.BodyText;
        var bodyToSend = _context.TamperedBodyText ?? _context.BodyText;

        string timestamp;
        string nonce;
        string signature;
        string canonicalString;

        if (reuseLastNonceTimestampAndSignature)
        {
            timestamp = _context.LastTimestamp!;
            nonce = _context.LastNonce!;
            signature = _context.LastSignature!;
            canonicalString = _context.LastCanonicalString!;
        }
        else
        {
            timestamp = _context.TimestampOverride ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            nonce = _context.NonceOverride ?? Guid.NewGuid().ToString("N");

            var bodyHashHex = SignatureTestHelper.ComputeSha256Hex(method == "POST" ? bodyToSign : string.Empty);
            canonicalString = SignatureTestHelper.BuildCanonicalString(method, basePath, timestamp, nonce, apiKey, bodyHashHex);
            signature = SignatureTestHelper.ComputeHmacSha256Hex(canonicalString, secret);

            if (_context.SignatureTransform is not null)
            {
                signature = _context.SignatureTransform(signature);
            }

            if (_context.SignatureOverride is not null)
            {
                signature = _context.SignatureOverride;
            }
        }

        var path = basePath + (_context.QueryString ?? string.Empty);

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (!_context.HeadersToOmit.Contains("X-Api-Key"))
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        if (!_context.HeadersToOmit.Contains("X-Timestamp"))
        {
            request.Headers.Add("X-Timestamp", timestamp);
        }

        if (!_context.HeadersToOmit.Contains("X-Nonce"))
        {
            request.Headers.Add("X-Nonce", nonce);
        }

        if (!_context.HeadersToOmit.Contains("X-Signature"))
        {
            request.Headers.Add("X-Signature", signature);
        }

        if (method == "POST")
        {
            request.Content = new StringContent(bodyToSend, Encoding.UTF8, "application/json");
        }

        var client = TestRunHooks.Factory.CreateClient();
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _context.LastResponse = response;
        _context.LastResponseBody = responseBody;
        _context.LastMethod = method;
        _context.LastBasePath = basePath;
        _context.LastTimestamp = timestamp;
        _context.LastNonce = nonce;
        _context.LastSignature = signature;
        _context.LastCanonicalString = canonicalString;
    }
}
