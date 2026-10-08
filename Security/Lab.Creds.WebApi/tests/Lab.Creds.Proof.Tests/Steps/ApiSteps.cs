using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Lab.Creds.Proof.Tests.Support;
using Reqnroll;
using Xunit;

namespace Lab.Creds.Proof.Tests.Steps;

[Binding]
public sealed class ApiSteps(ScenarioState state, TokenSteps tokenSteps)
{
    private HttpResponseMessage? _response;
    private string? _body;

    [When("該服務以同一憑證和 token 對 API 提交合作廠商資料")]
    public Task WhenSameCertificate() => Submit(state.ClientCertificate, state.AccessToken);

    [When("攻擊者只帶 token 且不出示憑證對 API 提交合作廠商資料")]
    public Task WhenNoCertificate() => Submit(null, state.AccessToken);

    [When("持有 {string} 憑證的呼叫端帶著 partner-a 的 token 對 API 提交合作廠商資料")]
    public Task WhenOtherRegisteredCertificate(string clientId)
        => Submit(clientId == "partner-b" ? ProofEnvironment.PartnerB : throw new ArgumentException(clientId), state.AccessToken);

    [When("持有未註冊憑證的呼叫端帶著 partner-a 的 token 對 API 提交合作廠商資料")]
    public Task WhenStrangerCertificate() => Submit(ProofEnvironment.Stranger, state.AccessToken);

    [When("該服務只出示憑證而不帶 token 對 API 提交合作廠商資料")]
    public Task WhenNoToken() => Submit(state.ClientCertificate, null);

    [Given("該 token 目前可以成功呼叫 API")]
    public async Task GivenTokenWorks()
    {
        await Submit(state.ClientCertificate, state.AccessToken);
        Assert.True(_response!.StatusCode == HttpStatusCode.Accepted, _body);
    }

    [When("授權伺服器撤銷該 token")]
    public async Task WhenRevoke()
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        using var scope = ProofEnvironment.AuthServer.Services.CreateAsyncScope();
        var tokens = scope.ServiceProvider.GetRequiredService<OpenIddict.Abstractions.IOpenIddictTokenManager>();
        var token = await tokens.FindByReferenceIdAsync(state.AccessToken!, cancellationToken);
        Assert.NotNull(token);
        Assert.True(await tokens.TryRevokeAsync(token!, cancellationToken));
    }

    [When("該 token 的有效期限已到期")]
    public async Task WhenExpired()
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        using var scope = ProofEnvironment.AuthServer.Services.CreateAsyncScope();
        var tokens = scope.ServiceProvider.GetRequiredService<OpenIddict.Abstractions.IOpenIddictTokenManager>();
        var token = await tokens.FindByReferenceIdAsync(state.AccessToken!, cancellationToken);
        Assert.NotNull(token);
        var descriptor = new OpenIddict.Abstractions.OpenIddictTokenDescriptor();
        await tokens.PopulateAsync(descriptor, token!, cancellationToken);
        descriptor.ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(-1);
        await tokens.UpdateAsync(token!, descriptor, cancellationToken);
    }

    [When("該服務重新以 mTLS 取得 token")]
    public async Task WhenReissue()
    {
        await tokenSteps.WhenRequestToken();
        tokenSteps.ThenBearer();
    }

    [Then("該服務以新 token 對 API 提交合作廠商資料回應 202")]
    public async Task ThenNewTokenAccepted()
    {
        await Submit(state.ClientCertificate, state.AccessToken);
        ThenStatus(202);
    }

    [When("該服務以同一憑證與無效 token 對 API 提交合作廠商資料")]
    public Task WhenInvalidToken() => Submit(state.ClientCertificate, "not-a-valid-token");

    [When("該服務以同一憑證和 token 對目標為 {string} 的另一個 API 提交合作廠商資料")]
    public Task WhenOtherTarget(string audience)
        => Submit(state.ClientCertificate, state.AccessToken, audience == "inventory-api" ? ProofEnvironment.OtherApiUri : throw new ArgumentException(audience));

    [When("沒有出示憑證的呼叫端以 partner-a 的 client_id 與 client_secret 對 token 端點發出請求")]
    public Task WhenTokenClientSecret() => RequestToken(null, new KeyValuePair<string, string>("client_secret", "guessed-secret"));

    [Then("同一憑證與 token 再次呼叫 API 回應 401")]
    public async Task ThenRevokedRejected()
    {
        await Submit(state.ClientCertificate, state.AccessToken);
        ThenStatus(401);
    }

    [Then("API 回應 202 並回報已驗證 Client 為 {string}")]
    public void ThenAccepted(string clientId)
    {
        Assert.True(_response!.StatusCode == HttpStatusCode.Accepted, _body);
        using var json = JsonDocument.Parse(_body!);
        Assert.Equal(clientId, json.RootElement.GetProperty("clientId").GetString());
    }

    [Then("API 回應 {int}")]
    public void ThenStatus(int status) => Assert.True((int)_response!.StatusCode == status, $"{(int)_response.StatusCode} {_body}");

    [When("沒有出示憑證的呼叫端以 partner-a 的 client_id 對 token 端點發出請求")]
    public Task WhenTokenNoCertificate() => RequestToken(null);

    [When("持有未註冊憑證的呼叫端以 partner-a 的 client_id 對 token 端點發出請求")]
    public Task WhenTokenStranger() => RequestToken(ProofEnvironment.Stranger);

    [Then("token 端點拒絕並回報 {string}")]
    public void ThenTokenRejected(string error)
    {
        Assert.False(_response!.IsSuccessStatusCode, _body);
        Assert.NotNull(_body);
        using var json = JsonDocument.Parse(_body!);
        Assert.Equal(error, json.RootElement.GetProperty("error").GetString());
    }

    private async Task RequestToken(X509Certificate2? certificate, params KeyValuePair<string, string>[] extra)
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        using var client = ProofEnvironment.CreateClient(ProofEnvironment.AuthServerUri, certificate);
        _response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new("grant_type", "client_credentials"),
            new("client_id", "partner-a"),
            new("scope", ProofDefaults.SubmitScope),
            ..extra
        ]), cancellationToken);
        _body = await _response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task Submit(X509Certificate2? certificate, string? accessToken, Uri? baseAddress = null)
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        baseAddress ??= ProofEnvironment.ApiUri;
        using var client = ProofEnvironment.CreateClient(baseAddress, certificate);
        using var request = SignedRequests.Submission(state.ClientId ?? "partner-a", baseAddress, accessToken).Build();
        _response = await client.SendAsync(request, cancellationToken);
        _body = await _response.Content.ReadAsStringAsync(cancellationToken);
    }
}
