using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace AuthSpike.Hosting;

/// <summary>spike 內所有 HTTPS 端點共用的信任根，只信任本 spike 的根 CA。</summary>
public sealed record SpikeTrust(X509Certificate2 CaCertificate)
{
    public bool ValidateServerChain(X509Certificate2? certificate)
    {
        if (certificate is null)
        {
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(CaCertificate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(certificate);
    }

    public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> ServerCertificateValidator
        => (_, certificate, _, _) => ValidateServerChain(certificate);
}
