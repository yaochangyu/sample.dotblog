using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>03 單：業務 API 的 HTTP Message Signatures 實際簽署與驗證情境。</summary>
[Binding]
public sealed class SignedBusinessRequestSteps
{
    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重（06 單）。</summary>
    private static string OrderJson() => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/03-signed-business-requests.md";

    private string? _token;
    private string? _orderId;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private string? _lastSignatureInput;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri(string path = "/orders", string query = "")
        => new($"https://localhost:{Runtime.OrdersApi.Port}{path}{query}");

    [Given("簽章呼叫端 {string} 已以 mTLS 取得 Token")]
    public async Task GivenSigningCallerHasToken(string clientId)
    {
        _token = await RequestTokenAsync(clientId);
        _token.Should().NotBeNullOrEmpty();
    }

    [Given("簽章呼叫端 {string} 已建立一筆訂單")]
    public async Task GivenSigningCallerCreatedOrder(string clientId)
    {
        var request = BuildCreateRequest();
        await SignAsync(request, clientId);
        await SendAsync(request);
        _status.Should().Be(HttpStatusCode.Created, _responseBody);
        _orderId.Should().NotBeNullOrEmpty();
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求")]
    public async Task WhenSigningCallerCreatesOrder(string clientId)
    {
        var request = BuildCreateRequest();
        await SignAsync(request, clientId);
        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求，但移除 {string} 標頭")]
    public async Task WhenSignedOrderIsSentWithoutHeader(string clientId, string headerName)
    {
        var request = BuildCreateRequest();
        await SignAsync(request, clientId);
        request.Headers.Remove(headerName);
        request.Content!.Headers.Remove(headerName);
        await SendAsync(request);
    }

    [When("持有 Token 的呼叫端未簽章送出建立訂單請求")]
    public async Task WhenUnsignedOrderIsSent()
    {
        await SendAsync(BuildCreateRequest());
    }

    [When("持有 Token 的呼叫端未簽章送出查詢訂單請求")]
    public async Task WhenUnsignedGetIsSent()
    {
        await SendAsync(BuildGetRequest(_orderId!));
    }

    [When("簽章呼叫端以 {string} 送出已簽章的查詢訂單請求")]
    public async Task WhenSigningCallerGetsOrder(string clientId)
    {
        var request = BuildGetRequest(_orderId!);
        await SignAsync(request, clientId);
        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的查詢訂單請求，但目標改為其他訂單編號")]
    public async Task WhenSignedGetTargetIsChanged(string clientId)
    {
        var request = BuildGetRequest(_orderId!);
        await SignAsync(request, clientId);
        request.RequestUri = OrdersUri($"/orders/{Guid.NewGuid()}");
        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的查詢訂單請求，查詢參數為 {string}，但送出時改為 {string}")]
    public async Task WhenSignedGetQueryIsChanged(string clientId, string signedQuery, string sentQuery)
    {
        var request = BuildGetRequest(_orderId!, $"?{signedQuery}");
        await SignAsync(request, clientId);
        request.RequestUri = OrdersUri($"/orders/{_orderId}", $"?{sentQuery}");
        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求，並改動 {string}")]
    public async Task WhenSignedOrderIsTampered(string clientId, string item)
    {
        var request = BuildCreateRequest();
        await SignAsync(request, clientId);

        switch (item)
        {
            case "目標":
                request.Headers.Host = "evil.example";
                break;
            case "Idempotency-Key 標頭":
                request.Headers.Remove("Idempotency-Key");
                request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
                break;
            case "Content-Type 標頭":
                request.Content!.Headers.Remove("Content-Type");
                request.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=utf-8");
                break;
            case "Body":
                ReplaceBodyKeepingSignedHeaders(request, OrderJson().Replace("\"quantity\":1", "\"quantity\":9"));
                break;
            case "授權 Token":
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await RequestTokenAsync(SpikeRuntime.OrdersClientId));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, "未知的竄改項目");
        }

        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出簽章已逾期的建立訂單請求")]
    public async Task WhenExpiredSignatureIsSent(string clientId)
    {
        var request = BuildCreateRequest();
        var now = DateTimeOffset.UtcNow;
        await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(clientId), now.AddSeconds(-120), now.AddSeconds(-60), SignedHttp.NewNonce());
        await SendAsync(request);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求，但以 {string} 的簽章金鑰簽署")]
    public async Task WhenOrderIsSignedWithOtherClientKey(string clientId, string keyOwner)
    {
        var request = BuildCreateRequest();
        var now = DateTimeOffset.UtcNow;
        await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(keyOwner), now, now.AddSeconds(60), SignedHttp.NewNonce());
        await SendAsync(request);
    }

    [Then("已簽章請求的 Signature-Input 涵蓋 {string} 與 {string}")]
    public void ThenSignatureInputCovers(string first, string second)
    {
        _lastSignatureInput.Should().NotBeNullOrEmpty();
        _lastSignatureInput.Should().Contain($"\"{first}\"");
        _lastSignatureInput.Should().Contain($"\"{second}\"");
    }

    [Then("簽章金鑰的公開部分與 {string} 用戶端憑證公開金鑰不同")]
    public void ThenSigningKeyDiffersFromMtlsKey(string clientId)
    {
        var signingPublicKey = Runtime.SigningKey(clientId).Key.ExportSubjectPublicKeyInfo();
        var certificatePublicKey = Runtime.ClientCertificate(clientId).PublicKey.EncodedKeyValue.RawData;
        signingPublicKey.Should().NotEqual(certificatePublicKey);
    }

    [Then("業務 API 回應 {int}")]
    public void ThenBusinessApiReturns(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("03 單狀態為 {string}")]
    public void ThenIssueStatusIs(string status)
    {
        IssueText.Should().Contain($"**Status:** {status}");
    }

    [Then("03 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("03 單第 {int} 項驗收標為略過而非完成")]
    public void ThenIssueCheckboxIsSkipped(int index)
    {
        var line = CheckboxAt(index);
        line.Should().StartWith("- [ ] ~~");
        line.Should().Contain("本 lab 不實作，略過（非完成）");
    }

    [Then("03 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }


    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".scratch")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("找不到專案根目錄（.scratch）。");
        }
    }

    private static string IssueText => File.ReadAllText(Path.Combine(RepoRoot, IssueRelativePath));

    private static string CheckboxAt(int index)
    {
        var checkboxes = IssueText.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^- \[[ x]\] "))
            .ToList();
        checkboxes.Should().HaveCountGreaterThanOrEqualTo(index);
        return checkboxes[index - 1];
    }

    private HttpRequestMessage BuildCreateRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri())
        {
            Content = new StringContent(OrderJson(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        return request;
    }

    private HttpRequestMessage BuildGetRequest(string orderId, string query = "")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, OrdersUri($"/orders/{orderId}", query));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return request;
    }

    private static Task SignAsync(HttpRequestMessage request, string clientId)
        => SignedHttp.SignNowAsync(request, Runtime.SigningKey(clientId));

    private async Task SendAsync(HttpRequestMessage request)
    {
        _lastSignatureInput = request.Headers.TryGetValues("Signature-Input", out var values) ? values.Single() : null;

        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(SpikeRuntime.OrdersClientId));
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Created)
        {
            using var document = JsonDocument.Parse(_responseBody);
            _orderId = document.RootElement.GetProperty("orderId").GetString();
        }
    }

    /// <summary>改動 Body 但保留原本已簽署的 Content-Type 與 Content-Digest 標頭。</summary>
    private static void ReplaceBodyKeepingSignedHeaders(HttpRequestMessage request, string newBody)
    {
        var contentType = request.Content!.Headers.GetValues("Content-Type").Single();
        var digest = request.Content.Headers.GetValues("Content-Digest").Single();
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(newBody));
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        request.Content.Headers.TryAddWithoutValidation("Content-Digest", digest);
    }

    private static async Task<string> RequestTokenAsync(string clientId)
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
            }));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

}
