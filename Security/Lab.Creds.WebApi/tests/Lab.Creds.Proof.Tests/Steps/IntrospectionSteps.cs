using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Lab.Creds.Proof.Tests.Support;
using Microsoft.IdentityModel.Tokens;
using Reqnroll;
using Xunit;

namespace Lab.Creds.Proof.Tests.Steps;

[Binding]
public sealed class IntrospectionSteps(ScenarioState state, TokenSteps tokenSteps)
{
    private HttpResponseMessage? _response;
    private string? _body;

    [Given("呼叫服務 {string} 已以其憑證取得 reference token")]
    public async Task GivenTokenIssued(string clientId)
    {
        tokenSteps.GivenRegisteredCertificate(clientId);
        await tokenSteps.WhenRequestToken();
        tokenSteps.ThenBearer();
    }

    [When("resource server {string} 以其已註冊憑證 mTLS 對 introspection 端點查詢該 token")]
    public Task WhenIntrospect(string clientId) => Introspect(clientId, ProofEnvironment.ApiIdentity);

    [When("持有未註冊憑證的 {string} 冒名對 introspection 端點查詢該 token")]
    public Task WhenIntrospectWithStranger(string clientId) => Introspect(clientId, ProofEnvironment.Stranger);

    [When("沒有出示任何憑證的 {string} 對 introspection 端點查詢該 token")]
    public Task WhenIntrospectWithoutCertificate(string clientId) => Introspect(clientId, null);

    [Then("introspection 回應 active 為 true")]
    public void ThenActive()
    {
        Assert.True(_response!.StatusCode == HttpStatusCode.OK, _body);
        using var json = JsonDocument.Parse(_body!);
        Assert.True(json.RootElement.GetProperty("active").GetBoolean(), _body);
    }

    [Then("introspection 回應 cnf 的 x5t#S256 等於呼叫服務憑證的 SHA-256 指紋")]
    public void ThenCnfMatchesCertificate()
    {
        using var json = JsonDocument.Parse(_body!);
        var cnf = json.RootElement.GetProperty("cnf").GetProperty("x5t#S256").GetString();
        var expected = Base64UrlEncoder.Encode(SHA256.HashData(state.ClientCertificate!.RawData));
        Assert.Equal(expected, cnf);
    }

    [Then("introspection 端點拒絕請求")]
    public void ThenRejected()
    {
        Assert.False(_response!.IsSuccessStatusCode, _body);
        Assert.DoesNotContain("\"active\"", _body!);
    }

    private async Task Introspect(string clientId, X509Certificate2? certificate)
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        using var client = ProofEnvironment.CreateClient(ProofEnvironment.AuthServerUri, certificate);
        _response = await client.PostAsync("/connect/introspect", new FormUrlEncodedContent(
        [
            new("client_id", clientId),
            new("token", state.AccessToken!)
        ]), cancellationToken);
        _body = await _response.Content.ReadAsStringAsync(cancellationToken);
    }
}
