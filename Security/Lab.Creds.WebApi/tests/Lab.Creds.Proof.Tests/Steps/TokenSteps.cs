using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Lab.Creds.Proof.Tests.Support;
using Reqnroll;
using Xunit;

namespace Lab.Creds.Proof.Tests.Steps;

[Binding]
public sealed class TokenSteps(ScenarioState state)
{
    [Given("呼叫服務 {string} 持有已註冊的 TLS 憑證")]
    public void GivenRegisteredCertificate(string clientId)
    {
        state.ClientId = clientId;
        state.ClientCertificate = clientId switch
        {
            "partner-a" => ProofEnvironment.PartnerA,
            "partner-b" => ProofEnvironment.PartnerB,
            _ => throw new ArgumentException(clientId)
        };
    }

    [When("該服務以 mTLS 對 token 端點發出 client_credentials 請求")]
    public async Task WhenRequestToken()
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;
        using var client = ProofEnvironment.CreateClient(ProofEnvironment.AuthServerUri, state.ClientCertificate);
        state.TokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new("grant_type", "client_credentials"),
            new("client_id", state.ClientId!),
            new("scope", ProofDefaults.SubmitScope)
        ]), cancellationToken);
        state.TokenBody = await state.TokenResponse.Content.ReadAsStringAsync(cancellationToken);
    }

    [Then("token 端點回應 200 且 token_type 為 Bearer")]
    public void ThenBearer()
    {
        Assert.True(state.TokenResponse!.StatusCode == HttpStatusCode.OK, state.TokenBody);
        using var json = JsonDocument.Parse(state.TokenBody!);
        Assert.Equal("Bearer", json.RootElement.GetProperty("token_type").GetString(), ignoreCase: true);
        state.AccessToken = json.RootElement.GetProperty("access_token").GetString();
    }

    [Then("token 回應的 expires_in 介於 {int} 到 {int} 秒")]
    public void ThenExpiresIn(int min, int max)
    {
        using var json = JsonDocument.Parse(state.TokenBody!);
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        Assert.InRange(expiresIn, min, max);
    }

    [Then("access_token 為 opaque reference token 而非 JWT")]
    public void ThenOpaque()
    {
        Assert.False(string.IsNullOrEmpty(state.AccessToken));
        Assert.DoesNotContain('.', state.AccessToken!);
    }
}

public sealed class ScenarioState
{
    public string? ClientId { get; set; }
    public X509Certificate2? ClientCertificate { get; set; }
    public HttpResponseMessage? TokenResponse { get; set; }
    public string? TokenBody { get; set; }
    public string? AccessToken { get; set; }
}
