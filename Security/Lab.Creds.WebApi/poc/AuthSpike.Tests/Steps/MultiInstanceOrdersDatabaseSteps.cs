using System.Net;
using System.Text.Json;
using AuthSpike.Hosting;
using AuthSpike.OrdersApi;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

[Binding]
public sealed class MultiInstanceOrdersDatabaseSteps
{
    private const string ClientId = "orders-client";
    private RealStorageTestHarness _harness = null!;
    private ApiResponse? _last;
    private List<ApiResponse> _race = [];
    private string? _lastOrderId;
    private string? _lastIdempotencyKey;
    private string? _lastItem;
    private int _lastQuantity;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("真實儲存測試環境已就緒且業務 API 連線至 {string}")]
    public async Task GivenRealStorageWithOrdersDb(string serviceName)
    {
        _harness = await RealStorageTestHarness.GetAsync();
        var connectionString = _harness.OrdersDbConnectionString;
        await SpikeEnvironment.UsePostgresOrdersAsync(connectionString);

        // 啟動第二個執行個體共用此資料庫
        var secondaryPort = SpikeEnvironment.FreeTcpPort();
        await Runtime.StartSecondaryOrdersApiAsync(secondaryPort);
    }

    [When("簽章呼叫端以 10 組新簽章與新 Idempotency-Key 併發送出訂單 {string}，品項 {string}，數量 {int}，交替送往真實資料庫的兩個執行個體")]
    public async Task WhenConcurrentRaceToBothInstances(string reference, string item, int quantity)
    {
        var primaryPort = Runtime.OrdersApi.Port;
        var secondaryPort = Runtime.SecondaryOrdersApi!.Port;

        var tasks = Enumerable.Range(0, 10).Select(async index =>
        {
            var key = Guid.NewGuid().ToString();
            var port = index % 2 == 0 ? primaryPort : secondaryPort;
            return await SendOrderAsync(key, reference, item, quantity, port);
        });

        _race = (await Task.WhenAll(tasks)).ToList();
    }

    [Then("併發回應只包含 201 或 409")]
    public void ThenConcurrentResponseOnly201Or409()
    {
        _race.Should().NotBeEmpty();
        _race.Select(r => r.Status).Should().OnlyContain(s => s == HttpStatusCode.Created || s == HttpStatusCode.Conflict);
    }

    [Then("201 回應的次數剛好為 1")]
    public void ThenCreatedCountIsExactlyOne()
    {
        _race.Count(r => r.Status == HttpStatusCode.Created).Should().Be(1);
    }

    [Then("訂單 {string} 於資料庫中的業務訂單數為 {int}")]
    public void ThenOrderReferenceCountInDatabase(string reference, int count)
    {
        Runtime.Orders.CountByOrderReference(ClientId, reference).Should().Be(count);
    }

    [Given("簽章呼叫端送出訂單 {string}，品項 {string}，數量 {int}")]
    [When("簽章呼叫端送出訂單 {string}，品項 {string}，數量 {int}")]
    public async Task WhenSendOrder(string reference, string item, int quantity)
    {
        var key = Guid.NewGuid().ToString();
        _lastIdempotencyKey = key;
        _lastItem = item;
        _lastQuantity = quantity;
        _last = await SendOrderAsync(key, reference, item, quantity, Runtime.OrdersApi.Port);
        if (_last.Status == HttpStatusCode.Created)
        {
            _lastOrderId = JsonDocument.Parse(_last.Body).RootElement.GetProperty("orderId").GetString();
        }
    }

    [When("業務 API 重新啟動")]
    public async Task WhenOrdersApiRestarted()
    {
        var connectionString = _harness.OrdersDbConnectionString;
        await SpikeEnvironment.RestoreDefaultTokensAsync();
        await SpikeEnvironment.UsePostgresOrdersAsync(connectionString);
    }

    [Then("查詢訂單 {string} 仍然存在且狀態為有效")]
    public void ThenQueryOrderStillExistsAndActive(string reference)
    {
        Runtime.Orders.TryGetByOrderReference(ClientId, reference, out var order).Should().BeTrue();
        order!.Status.Should().Be("active");
        order.Request.Item.Should().Be(_lastItem);
        order.Request.Quantity.Should().Be(_lastQuantity);
    }

    [Given("簽章呼叫端以 Idempotency-Key {string} 送出訂單 {string}，品項 {string}，數量 {int}")]
    [When("簽章呼叫端以 Idempotency-Key {string} 送出訂單 {string}，品項 {string}，數量 {int}")]
    public async Task WhenSendOrderWithKey(string key, string reference, string item, int quantity)
    {
        _lastIdempotencyKey = key;
        _lastItem = item;
        _lastQuantity = quantity;
        _last = await SendOrderAsync(key, reference, item, quantity, Runtime.OrdersApi.Port);
    }

    private CommitHold? _hold;
    private Task<ApiResponse>? _backgroundOrder;

    [Given("業務 API 提交於寫入前暫停")]
    public void GivenCommitHeld()
    {
        _hold = Runtime.Orders.HoldNextCommit();
    }

    [When("簽章呼叫端在背景送出訂單 {string}，品項 {string}，數量 {int} 至主要執行個體")]
    public void WhenSendOrderBackground(string reference, string item, int quantity)
    {
        var key = Guid.NewGuid().ToString();
        _backgroundOrder = SendOrderAsync(key, reference, item, quantity, Runtime.OrdersApi.Port);
    }

    [When("等待業務提交進入處理中暫停")]
    public async Task WhenWaitForHold()
    {
        _hold.Should().NotBeNull();
        await _hold!.Entered.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [When("業務操作在資料庫中的租約已手動逾期")]
    public void WhenLeaseManuallyExpired()
    {
        Runtime.Orders.ExpireLeasesForTest();
    }

    [When("簽章呼叫端以新嘗試送出訂單 {string}，品項 {string}，數量 {int} 至第二個執行個體")]
    public async Task WhenSendOrderNewAttemptToSecondary(string reference, string item, int quantity)
    {
        var key = Guid.NewGuid().ToString();
        _last = await SendOrderAsync(key, reference, item, quantity, Runtime.SecondaryOrdersApi!.Port);
        // 放行背景暫停以讓背景嘗試退出
        _hold?.Release();
    }

    [When("簽章呼叫端以相同 Idempotency-Key 重送訂單 {string}，品項 {string}，數量 {int}")]
    public async Task WhenResendOrderWithSameKey(string reference, string item, int quantity)
    {
        _last = await SendOrderAsync(_lastIdempotencyKey!, reference, item, quantity, Runtime.OrdersApi.Port);
    }

    [Then("最近一次真實訂單請求回應 {int}")]
    public void ThenLastDbOrderReturns(int statusCode)
    {
        _last.Should().NotBeNull("尚未送出任何訂單請求");
        _last!.Status.Should().Be((HttpStatusCode)statusCode, _last.Body);
    }

    [Then("最近一次真實訂單請求標示為重播")]
    public void ThenLastDbOrderReplayed()
    {
        _last!.Replayed.Should().BeTrue("重播的既有結果應帶 Idempotent-Replayed 標頭");
    }

    private static async Task<ApiResponse> SendOrderAsync(string key, string reference, string item, int quantity, int port)
    {
        var body = JsonSerializer.Serialize(new { OrderReference = reference, Item = item, Quantity = quantity });
        var token = await RequestTokenAsync();
        var primaryPort = Runtime.OrdersApi.Port;

        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{primaryPort}/orders"),
            token,
            Runtime.SigningKey(ClientId),
            body,
            key);

        if (port != primaryPort)
        {
            request.RequestUri = new Uri($"https://localhost:{port}/orders");
            request.Headers.Host = $"localhost:{primaryPort}";
        }

        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        return await SignedHttp.SendAsync(client, request);
    }

    private static async Task<string> RequestTokenAsync()
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    [AfterScenario]
    public static async Task CleanupEnvironment()
    {
        await SpikeEnvironment.RestoreDefaultTokensAsync();
    }
}
