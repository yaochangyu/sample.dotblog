using System.Security.Cryptography.X509Certificates;

namespace Lab.Creds.Proof;

public sealed record ProofClient(
    string ClientId,
    X509Certificate2 PublicCertificate,
    IReadOnlyList<string> Scopes,
    bool CanIntrospect = false);

public sealed record AuthServerOptions(
    X509Certificate2 ServerCertificate,
    string ConnectionString,
    IReadOnlyList<ProofClient> Clients,
    string Audience = ProofDefaults.Audience);

public sealed record ApiOptions(
    X509Certificate2 ServerCertificate,
    Uri Authority,
    string IntrospectionClientId,
    X509Certificate2 IntrospectionClientCertificate,
    X509Certificate2Collection TrustedRoots,
    string Audience = ProofDefaults.Audience,
    GatewayTrustOptions? Gateway = null);

// When set, the API accepts connections only from the trusted Gateway (mTLS) and takes the original
// client certificate exclusively from the Gateway-set X-Forwarded-Client-Cert header.
public sealed record GatewayTrustOptions(X509Certificate2 GatewayCertificate);

public static class ProofDefaults
{
    public const string Audience = "partner-api";
    public const string SubmitScope = "partner.submit";
}
