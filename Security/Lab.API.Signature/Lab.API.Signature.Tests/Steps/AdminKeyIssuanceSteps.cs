using System.Text;
using Lab.API.Signature.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Reqnroll;

namespace Lab.API.Signature.Tests.Steps;

[Binding]
public class AdminKeyIssuanceSteps
{
    private readonly AdminScenarioContext _context;

    public AdminKeyIssuanceSteps(AdminScenarioContext context)
    {
        _context = context;
    }

    // ---------- Given ----------

    [Given(@"我帶正確的 X-Admin-Key")]
    public void GivenIHaveCorrectAdminKey()
    {
        _context.AdminKeyHeaderValue = AdminApiTestHelper.ValidAdminApiKey;
    }

    [Given(@"我帶錯誤的 X-Admin-Key")]
    public void GivenIHaveWrongAdminKey()
    {
        _context.AdminKeyHeaderValue = AdminApiTestHelper.InvalidAdminApiKey;
    }

    [Given(@"我沒有帶 X-Admin-Key")]
    public void GivenIHaveNoAdminKey()
    {
        _context.AdminKeyHeaderValue = null;
    }

    [Given(@"系統執行在非 Development 環境")]
    public void GivenSystemRunsInNonDevelopmentEnvironment()
    {
        _context.UseProductionEnvironment = true;
        _context.AdminKeyHeaderValue = AdminApiTestHelper.ValidAdminApiKey;
    }

    [Given(@"我已經核發了一組新的 Client，ClientName 為 ""(.*)""")]
    public async Task GivenIHaveIssuedANewClient(string clientName)
    {
        await SendCreateClientRequestAsync(clientName);
    }

    // ---------- When ----------

    [When(@"^我送出 POST /api/admin/clients 請求，ClientName 為 ""(.*)""$")]
    public Task WhenISendCreateClientRequest(string clientName) => SendCreateClientRequestAsync(clientName);

    [When(@"^我送出 GET /api/admin/clients 請求$")]
    public async Task WhenISendListClientsRequest()
    {
        var client = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clients");
        AddAdminKeyHeader(request);

        var response = await client.SendAsync(request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();
    }

    // ---------- Then ----------

    [Then(@"Admin 回應狀態碼應該是 (\d+)")]
    public void ThenAdminStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, (int)_context.LastResponse!.StatusCode);
    }

    [Then(@"回應內容應該包含 clientName ""(.*)""")]
    public void ThenResponseShouldContainClientName(string clientName)
    {
        Assert.Contains($"\"clientName\":\"{clientName}\"", _context.LastResponseBody);
    }

    [Then(@"回應內容應該包含 apiKey 欄位")]
    public void ThenResponseShouldContainApiKeyField()
    {
        Assert.Contains("\"apiKey\":", _context.LastResponseBody);
    }

    [Then(@"回應內容應該包含 secret 欄位")]
    public void ThenResponseShouldContainSecretField()
    {
        Assert.Contains("\"secret\":", _context.LastResponseBody);
    }

    [Then(@"回應內容不應該包含 secret 欄位")]
    public void ThenResponseShouldNotContainSecretField()
    {
        Assert.DoesNotContain("\"secret\":", _context.LastResponseBody);
    }

    // ---------- Helpers ----------

    private async Task SendCreateClientRequestAsync(string clientName)
    {
        var client = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clients");
        AddAdminKeyHeader(request);
        request.Content = new StringContent(
            $$"""{"clientName":"{{clientName}}"}""", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();
    }

    private void AddAdminKeyHeader(HttpRequestMessage request)
    {
        if (_context.AdminKeyHeaderValue is not null)
        {
            request.Headers.Add("X-Admin-Key", _context.AdminKeyHeaderValue);
        }
    }

    private HttpClient CreateHttpClient()
    {
        if (_context.UseProductionEnvironment)
        {
            return TestRunHooks.Factory
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
                .CreateClient();
        }

        return TestRunHooks.Factory.CreateClient();
    }
}
