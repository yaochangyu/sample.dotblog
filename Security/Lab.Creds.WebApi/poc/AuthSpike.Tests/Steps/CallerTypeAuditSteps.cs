using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Audit;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AuthSpike.Verification;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>07 單：稽核區分呼叫者類型（AC-D13）。全部經真實 TLS、真實 password grant、真實內省與已簽章的服務請求。</summary>
[Binding]
public sealed class CallerTypeAuditSteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/07-caller-type-audit.md";
    private const string DefaultItem = "book";

    private readonly Dictionary<string, string> _correlations = new();
    private string? _userToken;
    private string? _serviceToken;
    private string? _lastCorrelation;
    private string? _lastUserNonce;
    private Guid? _lastOrderId;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("類型稽核票使用者 {string} 取得使用者 Token")]
    public async Task GivenUserObtainsToken(string username)
    {
        _userToken = await RequestUserTokenAsync(username);
    }

    [Given("類型稽核票測試使用者 {string} 被停用")]
    public void GivenTestUserIsDisabled(string username)
    {
        Runtime.Registry.DisableUser(username);
    }

    [Given("類型稽核票稽核紀錄暫時無法寫入")]
    public void GivenAuditWritesFail()
    {
        Runtime.AuditLog.SimulateWriteFailure = true;
    }

    [When("類型稽核票服務 {string} 以已簽章請求建立訂單")]
    public async Task WhenServiceCreatesSignedOrder(string clientId)
    {
        _serviceToken = await RequestServiceTokenAsync(clientId);
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post, OrdersUri, _serviceToken, Runtime.SigningKey(clientId), OrderJson(DefaultItem), Guid.NewGuid().ToString());
        await SendAsync(client, request);
    }

    [When("類型稽核票服務 {string} 以已簽章請求查詢使用者專屬訂單")]
    public async Task WhenServiceQueriesUserOnlyOrder(string clientId)
    {
        _serviceToken ??= await RequestServiceTokenAsync(clientId);
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Get, new Uri($"{OrdersUri}/me/{Guid.NewGuid()}"), _serviceToken, Runtime.SigningKey(clientId));
        await SendAsync(client, request);
    }

    [When("類型稽核票使用者以使用者 Token 建立訂單")]
    public Task WhenUserCreatesOrder() => UserCreateAsync(DefaultItem, nonce: null);

    [When("類型稽核票使用者以使用者 Token 建立訂單，品項為 {string}")]
    public Task WhenUserCreatesOrderWithItem(string item) => UserCreateAsync(item, nonce: null);

    [When("類型稽核票使用者以相同 X-Request-Nonce 重送建立訂單")]
    public Task WhenUserResendsWithSameNonce()
    {
        _lastUserNonce.Should().NotBeNullOrEmpty("必須先送出使用者建立訂單請求");
        return UserCreateAsync(DefaultItem, _lastUserNonce);
    }

    [When("類型稽核票使用者以使用者 Token 讀取剛建立的訂單")]
    public async Task WhenUserReadsCreatedOrder()
    {
        _lastOrderId.Should().NotBeNull("必須先成功建立訂單");
        _userToken.Should().NotBeNullOrEmpty();
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{OrdersUri}/me/{_lastOrderId}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        await SendAsync(client, request);
    }

    [When("類型稽核票把最近一次請求的關聯識別記為 {string}")]
    public void WhenNameLastCorrelation(string name)
    {
        _lastCorrelation.Should().NotBeNullOrEmpty("最近一次回應應帶 X-Correlation-Id");
        _correlations[name] = _lastCorrelation!;
    }

    [Then("類型稽核票最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("類型稽核票最近一次請求回應 {int} 且錯誤代碼為 {string}")]
    public void ThenLastRequestReturnsWithError(int statusCode, string errorCode)
    {
        ThenLastRequestReturns(statusCode);
        ErrorCodeOf(_lastBody).Should().Be(errorCode, _lastBody);
    }

    [Then("類型稽核票業務 API 建立訂單總數為 {int}")]
    public void ThenOrderCountIs(int count)
    {
        Runtime.Orders.Count.Should().Be(count);
    }

    /// <summary>表格第一列為欄位名稱與期望值（「空」表示欄位為 null），其餘列同格式。</summary>
    [Then("類型稽核票紀錄 {string} 為:")]
    public void ThenNamedRecordMatches(string name, DataTable table)
    {
        var record = NamedRecord(name);
        var rows = new[] { table.Header.ToArray() }.Concat(table.Rows.Select(row => row.Values.ToArray()));
        foreach (var cells in rows)
        {
            cells.Should().HaveCount(2, "每列為「欄位 | 期望值」");
            var expected = cells[1] == "空" ? null : cells[1];
            FieldOf(record, cells[0]).Should().Be(expected, $"紀錄 {name} 的欄位 {cells[0]}");
        }
    }

    [Then("類型稽核票稽核紀錄全部不含使用者 Token 與服務 Token")]
    public void ThenAuditDoesNotContainAnyToken()
    {
        var audit = SerializedAudit();
        _userToken.Should().NotBeNullOrEmpty();
        audit.Should().NotContain(_userToken!);
        if (_serviceToken is not null)
        {
            audit.Should().NotContain(_serviceToken);
        }
    }

    [Then("類型稽核票稽核紀錄全部不含 {string}")]
    public void ThenAuditDoesNotContainText(string text)
    {
        SerializedAudit().Should().NotContain(text);
    }

    [Then("類型稽核票稽核紀錄全部不含 {string} 的簽章私鑰")]
    public void ThenAuditDoesNotContainSigningKey(string clientId)
    {
        var key = Runtime.SigningKey(clientId).Key;
        var parameters = key.ExportParameters(includePrivateParameters: true);
        var audit = SerializedAudit();
        audit.Should().NotContain(Convert.ToBase64String(parameters.D!));
        audit.Should().NotContain(Convert.ToBase64String(key.ExportPkcs8PrivateKey()));
    }

    [Then("類型稽核票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("類型稽核票的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    private async Task UserCreateAsync(string item, string? nonce)
    {
        _userToken.Should().NotBeNullOrEmpty("必須先取得使用者 Token");
        _lastUserNonce = nonce ?? SignedHttp.NewNonce();
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(OrderJson(item), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.TryAddWithoutValidation("X-Request-Nonce", _lastUserNonce);
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        await SendAsync(client, request);
    }

    private async Task SendAsync(HttpClient client, HttpRequestMessage request)
    {
        using var response = await client.SendAsync(request);
        _lastStatus = response.StatusCode;
        _lastBody = await response.Content.ReadAsStringAsync();
        _lastCorrelation = response.Headers.TryGetValues("X-Correlation-Id", out var correlation) ? correlation.Single() : null;
        if (response.StatusCode == HttpStatusCode.Created)
        {
            _lastOrderId = JsonDocument.Parse(_lastBody).RootElement.GetProperty("orderId").GetGuid();
        }
    }

    private SecurityAuditRecord NamedRecord(string name)
    {
        _correlations.Should().ContainKey(name, "必須先以「把最近一次請求的關聯識別記為」命名");
        var records = Runtime.AuditLog.Snapshot().Where(record => record.CorrelationId == _correlations[name]).ToList();
        records.Should().ContainSingle("每筆請求應恰有一筆稽核紀錄");
        return records[0];
    }

    private static string? FieldOf(SecurityAuditRecord record, string field) => field switch
    {
        "結果" => record.Outcome,
        "原因" => record.Reason,
        "已驗證呼叫者類型" => record.VerifiedCallerType,
        "已驗證身分" => record.VerifiedCallerIdentity,
        "已驗證 Client" => record.VerifiedClientId,
        "未驗證呼叫者類型" => record.UnverifiedTokenCallerType,
        "未驗證身分" => record.UnverifiedTokenIdentity,
        "未驗證 Client" => record.UnverifiedTokenClientId,
        "最終業務結果" => record.ResultStatus?.ToString(),
        _ => throw new InvalidOperationException($"未知的稽核欄位：{field}"),
    };

    /// <summary>序列化全部稽核紀錄；要求至少有一筆紀錄，避免空紀錄讓「不含」檢查空轉通過。</summary>
    private static string SerializedAudit()
    {
        var records = Runtime.AuditLog.Snapshot();
        records.Should().NotBeEmpty("必須先有稽核紀錄，才能檢查其內容");
        return JsonSerializer.Serialize(records);
    }

    private static string OrderJson(string item)
        => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"{{item}}","quantity":1}""";

    private static string? ErrorCodeOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }

    private static async Task<string> RequestUserTokenAsync(string username)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = UserCallers.ClientId,
                ["username"] = username,
                ["password"] = UserCallers.TestUsers[username],
            }));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task<string> RequestServiceTokenAsync(string clientId)
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
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
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
}
