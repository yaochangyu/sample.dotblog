using System.Net;
using System.Net.Http.Headers;
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

/// <summary>
/// 10 單：完整接入流程與規格驗收。所有步驟以同一組真實 TLS 端點（呼叫端直連業務 API）執行，
/// 並檢查 10 單的 BDD 情境對應表、診斷輸出與實作紀錄。
/// </summary>
[Binding]
public sealed class CompleteProtectionAcceptanceSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/10-complete-protection-acceptance.md";
    private const string OrderItem = "book";

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>每個 Scenario 使用不同的後綴，避免同一執行環境內的業務識別與 Idempotency Key 互相碰撞。</summary>
    private readonly string _scope = Guid.NewGuid().ToString("N")[..8];

    private readonly Dictionary<string, string> _tokens = new();
    private string? _orderId;
    private string? _orderOwner;
    private string? _createBody;
    private string? _createIdempotencyKey;
    private string? _createOrderReference;
    private SentSnapshot? _lastSignedCreate;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private bool _replayed;

    private sealed record SentSnapshot(
        HttpMethod Method,
        Uri Uri,
        (string Name, string[] Values)[] Headers,
        (string Name, string[] Values)[] ContentHeaders,
        byte[] Body,
        string ClientId);

    private static Uri OrdersUri(string path)
        => new($"https://localhost:{Runtime.OrdersApi.Port}{path}");

    private string Scoped(string name) => $"{name}-{_scope}";

    [Given("10 單的實作紀錄可讀取")]
    public void GivenIssueRecordIsReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Given("接入呼叫端 {string} 以 mTLS 取得 Token")]
    public async Task GivenCallerRequestsToken(string clientId)
    {
        _tokens[clientId] = await RequestTokenAsync(clientId);
        _tokens[clientId].Should().NotBeNullOrEmpty();
    }

    [Given("診斷紀錄擷取已於獨立執行環境開始")]
    public async Task GivenDiagnosticsCaptureStarts()
    {
        DiagnosticsCapture.Start();
        await SpikeEnvironment.UseIsolatedRuntimeAsync();
    }

    [Given("接入呼叫端以 {string} 簽章送出建立訂單 {string}，Idempotency-Key 為 {string}")]
    [When("接入呼叫端以 {string} 簽章送出建立訂單 {string}，Idempotency-Key 為 {string}")]
    public async Task WhenCallerCreatesSignedOrder(string clientId, string orderReference, string idempotencyKey)
    {
        _createOrderReference = Scoped(orderReference);
        _createIdempotencyKey = Scoped(idempotencyKey);
        _createBody = $$"""{"orderReference":"{{_createOrderReference}}","item":"{{OrderItem}}","quantity":1}""";
        var request = BuildCreateRequest(clientId, _createBody, _createIdempotencyKey);
        await SignAsync(request, clientId);
        await SendAsync(request, clientId);
    }

    [When("接入呼叫端以新 nonce 與相同 Idempotency-Key 重送上一筆訂單")]
    public async Task WhenCallerRetriesWithNewNonce()
    {
        var request = BuildCreateRequest(SpikeRuntime.OrdersClientId, _createBody!, _createIdempotencyKey!);
        await SignAsync(request, SpikeRuntime.OrdersClientId);
        await SendAsync(request, SpikeRuntime.OrdersClientId);
    }

    [When("接入呼叫端以 {string} 簽章查詢上一筆訂單")]
    public async Task WhenCallerGetsSignedOrder(string clientId)
    {
        var request = BuildRequest(HttpMethod.Get, clientId, $"/orders/{_orderId}");
        await SignAsync(request, clientId);
        await SendAsync(request, clientId);
    }

    [When("接入呼叫端以 {string} 簽章取消上一筆訂單")]
    public async Task WhenCallerCancelsSignedOrder(string clientId)
    {
        var request = BuildCancelRequest(clientId);
        await SignAsync(request, clientId);
        await SendAsync(request, clientId);
    }

    [When("接入呼叫端以 {string} 簽章取消上一筆訂單，不附 Idempotency-Key")]
    public async Task WhenCallerCancelsSignedOrderWithoutIdempotencyKey(string clientId)
    {
        var request = BuildCancelRequest(clientId);
        request.Headers.Remove("Idempotency-Key");
        await SignAsync(request, clientId);
        await SendAsync(request, clientId);
    }

    [When("接入呼叫端以 {string} 簽章取消訂單，但目標改為其他訂單編號")]
    public async Task WhenCallerCancelsWithChangedTarget(string clientId)
    {
        var request = BuildCancelRequest(clientId);
        await SignAsync(request, clientId);
        request.RequestUri = OrdersUri($"/orders/{Guid.NewGuid()}/cancel");
        await SendAsync(request, clientId);
    }

    [When("接入呼叫端重送同一份已簽章的建立訂單請求")]
    public async Task WhenCallerReplaysSignedCreate()
    {
        _lastSignedCreate.Should().NotBeNull();
        var replay = Rebuild(_lastSignedCreate!);
        await SendAsync(replay, _lastSignedCreate!.ClientId);
    }

    [When("接入呼叫端以 Token 未簽章送出 {string}")]
    public async Task WhenCallerSendsUnsigned(string operation)
    {
        var request = BuildOperation(operation, SpikeRuntime.OrdersClientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[SpikeRuntime.OrdersClientId]);
        await SendAsync(request, SpikeRuntime.OrdersClientId);
    }

    [When("接入呼叫端不附 Token，只以 X-Api-Key 標頭送出 {string}")]
    public async Task WhenCallerSendsApiKeyOnly(string operation)
    {
        var request = BuildOperation(operation, SpikeRuntime.OrdersClientId);
        request.Headers.TryAddWithoutValidation("X-Api-Key", "lab-api-key-without-authentication");
        await SendAsync(request, SpikeRuntime.OrdersClientId);
    }

    [When("接入呼叫端不附任何授權標頭送出 {string}")]
    public async Task WhenCallerSendsWithoutAuthorization(string operation)
    {
        await SendAsync(BuildOperation(operation, SpikeRuntime.OrdersClientId), SpikeRuntime.OrdersClientId);
    }

    [Then("接入回應為 {int}")]
    public void ThenCallerResponseIs(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("接入回應標示為重播")]
    public void ThenCallerResponseIsReplayed()
    {
        _replayed.Should().BeTrue();
    }

    [Then("接入回應的 clientId 為 {string}")]
    public void ThenCallerResponseClientIdIs(string clientId)
    {
        ResponseProperty("clientId").Should().Be(clientId);
    }

    [Then("接入回應的狀態為 {string}")]
    public void ThenCallerResponseStatusIs(string status)
    {
        ResponseProperty("status").Should().Be(status);
    }

    [Then("業務訂單參考 {string} 的訂單數為 {int}")]
    public void ThenOrderReferenceCountIs(string orderReference, int count)
    {
        Runtime.Orders.CountByOrderReference(SpikeRuntime.OrdersClientId, Scoped(orderReference)).Should().Be(count);
    }

    [Then("上一筆訂單的狀態為 {string}")]
    public void ThenPreviousOrderStatusIs(string status)
    {
        _orderId.Should().NotBeNullOrEmpty();
        Runtime.Orders.TryGet(Guid.Parse(_orderId!), _orderOwner!, out var order).Should().BeTrue();
        order!.Status.Should().Be(status);
    }

    [Then("診斷紀錄擷取已停止並寫完")]
    public async Task ThenDiagnosticsCaptureStops()
    {
        await DiagnosticsCapture.StopAsync();
    }

    [Then("診斷輸出包含業務 API 的請求紀錄")]
    public void ThenDiagnosticsContainRequestLog()
    {
        DiagnosticsCapture.Captured.Should().Contain("/orders");
    }

    [Then("診斷輸出不含本次取得的 access_token")]
    public void ThenDiagnosticsOmitToken()
    {
        var token = _tokens[SpikeRuntime.OrdersClientId];
        token.Should().NotBeNullOrEmpty();
        DiagnosticsCapture.Captured.Should().NotContain(token);
    }

    [Then("診斷輸出不含簽章標頭值與私鑰標記")]
    public void ThenDiagnosticsOmitSignatureAndKeys()
    {
        _lastSignedCreate.Should().NotBeNull();
        var signature = _lastSignedCreate!.Headers.Single(h => h.Name.Equals("Signature", StringComparison.OrdinalIgnoreCase)).Values.Single();
        DiagnosticsCapture.Captured.Should().NotContain(signature);
        DiagnosticsCapture.Captured.Should().NotContain("-----BEGIN");
    }

    [Then("10 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("10 單的實作紀錄不包含 {string}")]
    public void ThenIssueRecordOmits(string unexpected)
    {
        IssueText.Should().NotContain(unexpected);
    }

    [Then("10 單狀態為 {string}")]
    public void ThenIssueStatusIs(string status)
    {
        IssueText.Should().Contain($"**Status:** {status}");
    }

    [Then("10 單所有驗收項目已勾選")]
    public void ThenAllIssueCheckboxesChecked()
    {
        var checkboxes = IssueText.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^- \[[ x]\] "))
            .ToList();
        checkboxes.Should().HaveCount(9);
        checkboxes.Should().OnlyContain(line => line.StartsWith("- [x] "));
    }

    [Then("10 單的 {string} 對應表列出 {int} 列")]
    public void ThenMappingTableHasRows(string table, int count)
    {
        MappingRows(table).Should().HaveCount(count);
    }

    [Then("10 單的 {string} 對應表中每個情境名稱都存在於 feature 檔")]
    public void ThenMappingScenarioNamesExist(string table)
    {
        var scenarioNames = FeatureScenarioNames();
        var missing = MappingRows(table)
            .SelectMany(row => row[1].Split('；', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(name => !scenarioNames.Contains(name))
            .ToList();

        missing.Should().BeEmpty("對應表列出的情境名稱必須存在於 feature 檔");
    }

    /// <summary>讀取 10 單 «{key} 對應» 小節的資料列（略過表頭與分隔列）。</summary>
    private static List<string[]> MappingRows(string key)
    {
        var lines = IssueText.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        var heading = lines.FindIndex(line => line.Trim() == $"### {key} 對應");
        heading.Should().BeGreaterThanOrEqualTo(0, $"10 單必須有「### {key} 對應」小節");

        var rows = new List<string[]>();
        var dataRows = 0;
        for (var i = heading + 1; i < lines.Count && !lines[i].StartsWith('#'); i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('|'))
            {
                continue;
            }

            dataRows++;
            if (dataRows <= 2)
            {
                continue; // 表頭與 |---| 分隔列
            }

            rows.Add(line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray());
        }

        return rows;
    }

    private static HashSet<string> FeatureScenarioNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var featureDirectory = Path.Combine(RepoRoot, "poc", "AuthSpike.Tests", "Features");
        foreach (var file in Directory.EnumerateFiles(featureDirectory, "*.feature"))
        {
            foreach (var raw in File.ReadLines(file))
            {
                var line = raw.Trim();
                if (line.StartsWith("Scenario Outline:", StringComparison.Ordinal))
                {
                    names.Add(line["Scenario Outline:".Length..].Trim());
                }
                else if (line.StartsWith("Scenario:", StringComparison.Ordinal))
                {
                    names.Add(line["Scenario:".Length..].Trim());
                }
            }
        }

        return names;
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

    private string ResponseProperty(string name)
    {
        using var document = JsonDocument.Parse(_responseBody);
        return document.RootElement.GetProperty(name).GetString()!;
    }

    private HttpRequestMessage BuildOperation(string operation, string clientId) => operation switch
    {
        "建立訂單" => BuildCreateRequest(clientId, $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"{{OrderItem}}","quantity":1}""", Guid.NewGuid().ToString()),
        "查詢訂單" => BuildRequest(HttpMethod.Get, clientId, $"/orders/{_orderId}"),
        "取消訂單" => BuildCancelRequest(clientId),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "未知的業務入口"),
    };

    private HttpRequestMessage BuildRequest(HttpMethod method, string clientId, string path)
    {
        var request = new HttpRequestMessage(method, OrdersUri(path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[clientId]);
        return request;
    }

    /// <summary>取消訂單為有 Body 的 POST（Body 為 {}），簽章涵蓋 Idempotency-Key（見 openapi.yml 與 03 單規則）。</summary>
    private HttpRequestMessage BuildCancelRequest(string clientId)
    {
        var request = BuildRequest(HttpMethod.Post, clientId, $"/orders/{_orderId}/cancel");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        return request;
    }

    private HttpRequestMessage BuildCreateRequest(string clientId, string body, string idempotencyKey)
    {
        var request = BuildRequest(HttpMethod.Post, clientId, "/orders");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static Task SignAsync(HttpRequestMessage request, string clientId)
    {
        var now = DateTimeOffset.UtcNow;
        return BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(clientId), now, now.AddSeconds(60), Guid.NewGuid().ToString("N"));
    }

    private async Task SendAsync(HttpRequestMessage request, string clientId)
    {
        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/orders" && request.Headers.Contains("Signature"))
        {
            _lastSignedCreate = await SnapshotAsync(request, clientId);
        }

        using var client = CreateHttpClient(Runtime.ClientCertificate(clientId));
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
        _replayed = response.Headers.TryGetValues("Idempotent-Replayed", out var replayed) && replayed.Single() == "true";

        if (response.StatusCode == HttpStatusCode.Created && request.RequestUri!.AbsolutePath == "/orders")
        {
            _orderId = ResponseProperty("orderId");
            _orderOwner = clientId;
        }
        else if (response.StatusCode == HttpStatusCode.OK && request.RequestUri!.AbsolutePath.EndsWith("/cancel", StringComparison.Ordinal))
        {
            _orderId = ResponseProperty("orderId");
            _orderOwner = clientId;
        }
    }

    private static async Task<SentSnapshot> SnapshotAsync(HttpRequestMessage request, string clientId)
    {
        var body = request.Content is null ? Array.Empty<byte>() : await request.Content.ReadAsByteArrayAsync();
        return new SentSnapshot(
            request.Method,
            request.RequestUri!,
            request.Headers.Select(h => (h.Key, h.Value.ToArray())).ToArray(),
            request.Content?.Headers.Select(h => (h.Key, h.Value.ToArray())).ToArray() ?? [],
            body,
            clientId);
    }

    /// <summary>依已送出的快照重建請求（同一簽章、同一 nonce），用於模擬重送。</summary>
    private static HttpRequestMessage Rebuild(SentSnapshot snapshot)
    {
        var request = new HttpRequestMessage(snapshot.Method, snapshot.Uri);
        foreach (var (name, values) in snapshot.Headers)
        {
            request.Headers.TryAddWithoutValidation(name, values);
        }

        if (snapshot.Body.Length > 0)
        {
            request.Content = new ByteArrayContent(snapshot.Body);
            foreach (var (name, values) in snapshot.ContentHeaders)
            {
                request.Content.Headers.TryAddWithoutValidation(name, values);
            }
        }

        return request;
    }

    private static async Task<string> RequestTokenAsync(string clientId)
    {
        using var client = CreateHttpClient(Runtime.ClientCertificate(clientId));
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

    /// <summary>每次呼叫都使用新連線，確保 TLS 用戶端憑證依本次呼叫決定。</summary>
    private static HttpClient CreateHttpClient(X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true);
    }
}

/// <summary>
/// 診斷紀錄擷取：將程序的 Console 輸出（ASP.NET 請求與框架記錄）導入記憶體，供洩漏檢查使用。
/// 擷取期間使用獨立執行環境，確保新建立的主機會寫入被導向的 Console。
/// </summary>
internal static class DiagnosticsCapture
{
    private static TextWriter? _original;
    private static StringWriter? _buffer;

    public static string Captured { get; private set; } = string.Empty;

    public static void Start()
    {
        _buffer = new StringWriter();
        _original = Console.Out;
        Console.SetOut(TextWriter.Synchronized(_buffer));
    }

    /// <summary>停止擷取並釋放獨立執行環境（釋放主機時會寫完待寫記錄），之後還原 Console 與預設執行環境。</summary>
    public static async Task StopAsync()
    {
        if (_buffer is null)
        {
            return;
        }

        await SpikeEnvironment.RestoreDefaultTokensAsync();
        Console.Out.Flush();
        Console.SetOut(_original!);
        Captured = _buffer.ToString();
        _buffer = null;
    }
}

[Binding]
public static class DiagnosticsCaptureHooks
{
    /// <summary>Scenario 中途失敗時仍還原 Console，避免影響其他 Scenario。</summary>
    [AfterScenario("@diagnostics")]
    public static async Task RestoreAfterFailure()
    {
        await DiagnosticsCapture.StopAsync();
    }
}
