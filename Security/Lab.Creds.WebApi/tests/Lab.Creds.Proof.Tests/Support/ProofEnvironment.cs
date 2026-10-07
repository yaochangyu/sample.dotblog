using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Lab.Creds.Proof;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Testcontainers.PostgreSql;

namespace Lab.Creds.Proof.Tests.Support;

// One isolated PostgreSQL container, one OpenIddict authorization server and one resource API per test run,
// each on real Kestrel TLS listeners bound to loopback.
public static class ProofEnvironment
{
    private static PostgreSqlContainer? _postgres;
    private static WebApplication? _authServer;
    private static WebApplication? _api;
    private static WebApplication? _gatewayApi;
    private static EnvoyGateway? _envoy;

    public static TestCertificates Certificates { get; private set; } = null!;
    public static X509Certificate2 PartnerA { get; private set; } = null!;
    public static X509Certificate2 PartnerB { get; private set; } = null!;
    public static X509Certificate2 Stranger { get; private set; } = null!;
    public static X509Certificate2 ApiIdentity { get; private set; } = null!;
    public static Uri AuthServerUri { get; private set; } = null!;
    public static Uri ApiUri { get; private set; } = null!;
    public static Uri GatewayUri { get; private set; } = null!;
    public static Uri GatewayApiUri { get; private set; } = null!;
    public static WebApplication AuthServer => _authServer!;

    public static async Task StartAsync()
    {
        Certificates = new TestCertificates();
        PartnerA = Certificates.CreateClientCertificate("partner-a");
        PartnerB = Certificates.CreateClientCertificate("partner-b");
        Stranger = Certificates.CreateClientCertificate("stranger");
        ApiIdentity = Certificates.CreateClientCertificate("partner-api");

        _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _postgres.StartAsync();

        _authServer = ProofHosts.CreateAuthServer(new AuthServerOptions(
            Certificates.Server,
            _postgres.GetConnectionString(),
            [
                new ProofClient("partner-a", TestCertificates.PublicOnly(PartnerA), [ProofDefaults.SubmitScope]),
                new ProofClient("partner-b", TestCertificates.PublicOnly(PartnerB), [ProofDefaults.SubmitScope]),
                new ProofClient(ProofDefaults.Audience, TestCertificates.PublicOnly(ApiIdentity), [], CanIntrospect: true)
            ]));
        await _authServer.StartAsync();
        AuthServerUri = AddressOf(_authServer);

        _api = ProofHosts.CreateApi(new ApiOptions(
            Certificates.Server,
            AuthServerUri,
            ProofDefaults.Audience,
            ApiIdentity,
            [Certificates.Root]));
        await _api.StartAsync();
        ApiUri = AddressOf(_api);

        _gatewayApi = ProofHosts.CreateApi(new ApiOptions(
            Certificates.Server,
            AuthServerUri,
            ProofDefaults.Audience,
            ApiIdentity,
            [Certificates.Root],
            Gateway: new GatewayTrustOptions(TestCertificates.PublicOnly(Certificates.Gateway))));
        await _gatewayApi.StartAsync();
        GatewayApiUri = AddressOf(_gatewayApi);

        _envoy = new EnvoyGateway();
        await _envoy.StartAsync(EnvoyGateway.RenderConfig(Certificates, GatewayApiUri.Port, [PartnerA, PartnerB]));
        GatewayUri = _envoy.Uri;
    }

    public static async Task StopAsync()
    {
        if (_envoy is not null) await _envoy.DisposeAsync();
        if (_gatewayApi is not null) await _gatewayApi.DisposeAsync();
        if (_api is not null) await _api.DisposeAsync();
        if (_authServer is not null) await _authServer.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
        Certificates?.Dispose();
    }

    // Real TLS validation: the server certificate must chain to the generated test root.
    public static HttpClient CreateClient(Uri baseAddress, X509Certificate2? clientCertificate)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.RemoteCertificateValidationCallback = ValidateAgainstTestRoot;
        if (clientCertificate is not null)
        {
            handler.SslOptions.ClientCertificates = [clientCertificate];
        }

        return new HttpClient(handler) { BaseAddress = baseAddress };
    }

    private static bool ValidateAgainstTestRoot(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (certificate is null) return false;
        using var policyChain = new X509Chain();
        policyChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        policyChain.ChainPolicy.CustomTrustStore.Add(Certificates.Root);
        policyChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return policyChain.Build(new X509Certificate2(certificate)) && errors is SslPolicyErrors.None or SslPolicyErrors.RemoteCertificateChainErrors;
    }

    private static Uri AddressOf(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new Uri(address.Replace("127.0.0.1", "localhost").Replace("0.0.0.0", "localhost").Replace("[::]", "localhost"));
    }
}
