using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using AuthSpike.Audit;
using AuthSpike.Certificates;
using AuthSpike.OrdersApi;
using AuthSpike.Replay;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AuthSpike.Trust;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.IdentityModel.Tokens;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>07 單補強：以假的授權伺服器回傳異常的 introspection 回應，確認一律 fail closed 為 503。</summary>
[Binding]
public sealed class IntrospectionFaultSteps : IAsyncDisposable
{
    private WebApplication? _fakeAuthServer;
    private OrdersApiHost? _ordersApi;
    private HttpStatusCode? _status;
    private string _body = string.Empty;

    private static AuthSpike.Hosting.SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("查證服務以假的授權伺服器取代，introspection 行為為 {string}")]
    public async Task GivenFakeAuthServer(string behavior)
    {
        var serverCertificate = SpikeCertificates.IssueServerCertificate(Runtime.Trust.CaCertificate, "localhost");
        var authPort = SpikeEnvironment.FreeTcpPort();
        var issuer = $"https://localhost:{authPort}/";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(authPort, listen => listen.UseHttps(https =>
        {
            https.ServerCertificate = serverCertificate;
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.ClientCertificateValidation = static (_, _, _) => true;
        })));
        var app = builder.Build();
        app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer,
            introspection_endpoint = $"{issuer}connect/introspect",
            jwks_uri = $"{issuer}.well-known/jwks",
            token_endpoint = $"{issuer}connect/token",
            grant_types_supported = new[] { "client_credentials" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            token_endpoint_auth_methods_supported = new[] { "private_key_jwt", "self_signed_tls_client_auth" },
            introspection_endpoint_auth_methods_supported = new[] { "private_key_jwt", "self_signed_tls_client_auth" },
        }));
        var rsa = serverCertificate.GetRSAPublicKey()!.ExportParameters(false);
        var jwk = new { kty = "RSA", use = "sig", alg = "RS256", kid = "fake", n = Base64UrlEncoder.Encode(rsa.Modulus!), e = Base64UrlEncoder.Encode(rsa.Exponent!) };
        app.MapGet("/.well-known/jwks", () => Results.Json(new { keys = new[] { jwk } }));
        app.MapPost("/connect/introspect", () => behavior switch
        {
            "status-502" => Results.StatusCode(StatusCodes.Status502BadGateway),
            "status-500" => Results.StatusCode(StatusCodes.Status500InternalServerError),
            "bad-json" => Results.Content("{not-json", "application/json"),
            "empty" => Results.Content(string.Empty, "application/json"),
            "inactive" => Results.Json(new { active = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "未知的假授權伺服器行為"),
        });
        await app.StartAsync();
        _fakeAuthServer = app;

        _ordersApi = await OrdersApiHost.StartAsync(
            SpikeEnvironment.FreeTcpPort(),
            Runtime.Trust,
            serverCertificate,
            new Uri(issuer),
            AuthSpike.Hosting.SpikeRuntime.ResourceClientId,
            Runtime.ClientCertificate(AuthSpike.Hosting.SpikeRuntime.ResourceClientId),
            new VerificationKeyStore(),
            new NonceReplayStore(),
            new TrustRegistry(),
            TimeSpan.FromSeconds(10),
            new OrderStore(),
            new SecurityAuditLog());
    }

    [When("呼叫端持任意 Token 查詢訂單")]
    public async Task WhenCallerQueriesOrder()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(Runtime.ClientCertificate(AuthSpike.Hosting.SpikeRuntime.OrdersClientId));
        using var client = new HttpClient(handler, disposeHandler: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:{_ordersApi!.Port}/orders/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "any-token-value");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _body = await response.Content.ReadAsStringAsync();
    }

    [Then("查證故障情境的回應為 {int}")]
    public void ThenResponseIs(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _body);
        if (statusCode == 503)
        {
            _body.Should().Contain("verification_unavailable");
        }
    }

    [AfterScenario]
    public async Task Cleanup() => await DisposeAsync();

    public async ValueTask DisposeAsync()
    {
        if (_ordersApi is not null)
        {
            await _ordersApi.DisposeAsync();
            _ordersApi = null;
        }

        if (_fakeAuthServer is not null)
        {
            await _fakeAuthServer.StopAsync();
            await _fakeAuthServer.DisposeAsync();
            _fakeAuthServer = null;
        }
    }
}
