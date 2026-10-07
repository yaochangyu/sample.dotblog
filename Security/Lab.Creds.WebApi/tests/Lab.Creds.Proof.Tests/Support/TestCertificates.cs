using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Lab.Creds.Proof.Tests.Support;

// Test-only certificates generated at runtime; nothing is written to disk or committed.
public sealed class TestCertificates : IDisposable
{
    private readonly List<X509Certificate2> _owned = [];

    private readonly X509Certificate2 _rootSigner;

    public X509Certificate2 Root { get; }
    public X509Certificate2 Server { get; }
    public X509Certificate2 Gateway { get; }
    public X509Certificate2 UntrustedGateway { get; }

    public TestCertificates()
    {
        using var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var rootRequest = new CertificateRequest("CN=Lab.Creds.Proof Test Root", rootKey, HashAlgorithmName.SHA256);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        var root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(2));
        Root = Track(X509CertificateLoader.LoadCertificate(root.Export(X509ContentType.Cert)));

        _rootSigner = Track(root);
        Server = Track(CreateServerCertificate(root, "localhost"));
        Gateway = Track(CreateGatewayCertificate("envoy-gateway"));
        UntrustedGateway = Track(CreateGatewayCertificate("rogue-gateway"));
    }

    // Gateway identity used by Envoy as its upstream client certificate; separate from every partner certificate.
    public X509Certificate2 CreateGatewayCertificate(string commonName)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2")], false));
        var serial = RandomNumberGenerator.GetBytes(8);
        using var issued = request.Create(_rootSigner, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1), serial);
        return Reexport(issued.CopyWithPrivateKey(key));
    }

    public X509Certificate2 CreateServerCertificate(X509Certificate2 issuer, string dnsName)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={dnsName}", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(dnsName);
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        var serial = RandomNumberGenerator.GetBytes(8);
        using var issued = request.Create(issuer, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1), serial);
        return Reexport(issued.CopyWithPrivateKey(key));
    }

    // Self-signed certificate suitable for self_signed_tls_client_auth (client authentication EKU).
    public X509Certificate2 CreateClientCertificate(string commonName)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2")], false));
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        return Track(Reexport(cert));
    }

    public static X509Certificate2 PublicOnly(X509Certificate2 certificate)
        => X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));

    // Round-trip so the private key is usable for TLS on every platform.
    private static X509Certificate2 Reexport(X509Certificate2 certificate)
        => X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null);

    private X509Certificate2 Track(X509Certificate2 certificate)
    {
        _owned.Add(certificate);
        return certificate;
    }

    public void Dispose() => _owned.ForEach(certificate => certificate.Dispose());
}
