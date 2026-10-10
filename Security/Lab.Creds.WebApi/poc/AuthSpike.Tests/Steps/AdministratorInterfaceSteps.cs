using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>11 單：管理介面與管理員身分驗證。所有呼叫皆經真實 mTLS 與 HTTP 路徑，不以替身取代管理員驗證。</summary>
[Binding]
public sealed class AdministratorInterfaceSteps
{
    private const string ClientId = "orders-client";
    private const string AdminTrustListPath = "admin/trust-list";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/11-admin-interface-and-identity.md";

    private ApiResponse? _admin;
    private string? _baseline;
    private bool _tokenIssued;
    private string? _token;
    private ApiResponse? _business;
    private int _ordersBefore;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

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

    /// <summary>情境中使用的呼叫身分：無憑證、未登錄憑證、Client 憑證或管理員憑證。</summary>
    private static X509Certificate2? Certificate(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "Client 憑證" => Runtime.ClientCertificate(ClientId),
        "管理員憑證" => Runtime.AdministratorCertificate,
        _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "未知的呼叫身分"),
    };

    [Given("11 單的實作紀錄可讀取")]
    public void GivenIssueRecordReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Then("11 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("11 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    [Then("管理員憑證的指紋與 {string} 的 mTLS 憑證指紋不同")]
    public void ThenAdministratorThumbprintDiffers(string clientId)
    {
        Runtime.AdministratorCertificate.Thumbprint.Should().NotBe(Runtime.ClientCertificate(clientId).Thumbprint);
    }

    [Then("管理員憑證不在任何 Client 的信任登錄中")]
    public async Task ThenAdministratorCertificateNotRegisteredAsClient()
    {
        var thumbprints = RegisteredThumbprints(await TrustListBodyAsync());
        thumbprints.Should().Contain(Runtime.ClientCertificate(ClientId).Thumbprint);
        thumbprints.Should().NotContain(Runtime.AdministratorCertificate.Thumbprint);
    }

    [When("以 {string} 身分讀取管理介面的信任名單")]
    public async Task WhenReadsTrustList(string identity)
    {
        _admin = await SendAdminAsync(Certificate(identity), HttpMethod.Get, AdminTrustListPath);
    }

    [When("以 {string} 身分呼叫管理介面停用 {string}")]
    public async Task WhenCallsAdminToDisable(string identity, string clientId)
    {
        _admin = await SendAdminAsync(Certificate(identity), HttpMethod.Post, $"admin/clients/{clientId}/disable");
    }

    [Given("管理員已讀取信任名單作為基準")]
    public async Task GivenAdministratorReadsBaseline()
    {
        _baseline = await TrustListBodyAsync();
    }

    [Then("管理介面回應為 {int}")]
    public void ThenAdminResponseIs(int expected)
    {
        _admin.Should().NotBeNull("情境應先呼叫管理介面");
        ((int)_admin!.Status).Should().Be(expected, _admin.Body);
    }

    [Then("信任名單與基準相同")]
    public async Task ThenTrustListUnchanged()
    {
        _baseline.Should().NotBeNull();
        (await TrustListBodyAsync()).Should().Be(_baseline);
    }

    [Then("信任名單包含 {string}")]
    public async Task ThenTrustListContainsClient(string clientId)
    {
        ClientEntry(await TrustListBodyAsync(), clientId).GetProperty("clientId").GetString().Should().Be(clientId);
    }

    [Then("信任名單中 {string} 為 {string}")]
    public async Task ThenTrustListClientState(string clientId, string state)
    {
        var enabled = ClientEntry(await TrustListBodyAsync(), clientId).GetProperty("enabled").GetBoolean();
        enabled.Should().Be(state switch
        {
            "啟用" => true,
            "停用" => false,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "未知的 Client 狀態"),
        });
    }

    [When("以 {string} 身分要求 {string} 的 Token")]
    public async Task WhenIdentityRequestsToken(string identity, string clientId)
    {
        (_tokenIssued, _) = await RequestTokenAsync(Certificate(identity), clientId);
    }

    [Then("管理員的 Token 要求未核發")]
    public void ThenTokenNotIssued()
    {
        _tokenIssued.Should().BeFalse();
    }

    [Then("以 {string} 的 mTLS 憑證向授權伺服器要求 Token 被拒絕")]
    public async Task ThenClientCertificateTokenRejected(string identity)
    {
        var (issued, _) = await RequestTokenAsync(Certificate(identity), ClientId);
        issued.Should().BeFalse();
    }

    [Given("管理介面情境中 {string} 以 mTLS 取得 Token")]
    public async Task GivenClientObtainsToken(string clientId)
    {
        var (issued, token) = await RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue("Client 憑證應能取得 Token");
        _token = token;
    }

    [When("以 {string} 身分持該 Token 送出建立訂單請求")]
    public async Task WhenIdentitySendsOrderWithToken(string identity)
    {
        _ordersBefore = Runtime.OrdersApi.OrderCount;
        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            _token,
            Runtime.SigningKey(ClientId),
            json,
            Guid.NewGuid().ToString());
        using var client = SignedHttp.CreateClient(Runtime, Certificate(identity));
        _business = await SignedHttp.SendAsync(client, request);
    }

    [Then("業務 API 回應為 {int}")]
    public void ThenBusinessResponseIs(int expected)
    {
        _business.Should().NotBeNull("情境應先送出業務請求");
        ((int)_business!.Status).Should().Be(expected, _business.Body);
    }

    [Then("業務 API 的訂單數未增加")]
    public void ThenOrderCountUnchanged()
    {
        Runtime.OrdersApi.OrderCount.Should().Be(_ordersBefore);
    }

    internal static async Task<ApiResponse> SendAdminAsync(X509Certificate2? certificate, HttpMethod method, string path)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate);
        using var request = new HttpRequestMessage(method, new Uri($"https://localhost:{Runtime.AuthServer.Port}/{path}"));
        return await SignedHttp.SendAsync(client, request);
    }

    /// <summary>以管理員憑證讀取信任名單原始回應；管理員驗證本身也在此被檢查（必須 200）。</summary>
    internal static async Task<string> TrustListBodyAsync()
    {
        var response = await SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AdminTrustListPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        return response.Body;
    }

    private static JsonElement ClientEntry(string body, string clientId)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == clientId)
            .Clone();
    }

    internal static List<string> RegisteredThumbprints(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("clients").EnumerateArray()
            .SelectMany(client => client.GetProperty("certificateThumbprints").EnumerateArray())
            .Select(thumbprint => thumbprint.GetString()!)
            .ToList();
    }

    internal static async Task<(bool Issued, string? Token)> RequestTokenAsync(X509Certificate2? certificate, string clientId)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
            }));

        if (!response.IsSuccessStatusCode)
        {
            return (false, null);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (true, document.RootElement.GetProperty("access_token").GetString());
    }
}
