using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AuthSpike.Certificates;

/// <summary>
/// 僅供 spike 使用的測試憑證產生器。憑證與私鑰只存在記憶體中，不寫入磁碟。
/// </summary>
public static class SpikeCertificates
{
    private static readonly Oid ClientAuthOid = new("1.3.6.1.5.5.7.3.2");
    private static readonly Oid ServerAuthOid = new("1.3.6.1.5.5.7.3.1");

    /// <summary>自簽根 CA，僅用於簽發 TLS 伺服器憑證及模擬「其他 CA」。</summary>
    public static X509Certificate2 CreateRootCertificateAuthority(string commonName)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return Materialize(certificate, key);
    }

    /// <summary>伺服器 TLS 憑證（serverAuth + SAN），由根 CA 簽發。</summary>
    public static X509Certificate2 IssueServerCertificate(X509Certificate2 authority, string dnsName)
    {
        using var authorityKey = authority.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("CA certificate must contain a private key.");
        using var key = RSA.Create(2048);

        var request = new CertificateRequest($"CN={dnsName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ServerAuthOid], true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(authority, includeKeyIdentifier: true, includeIssuerAndSerial: false));
        var sans = new SubjectAlternativeNameBuilder();
        sans.AddDnsName(dnsName);
        request.CertificateExtensions.Add(sans.Build());

        var signer = X509SignatureGenerator.CreateForRSA(authorityKey, RSASignaturePadding.Pkcs1);
        var leaf = request.Create(authority.SubjectName, signer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30), RandomNumberGenerator.GetBytes(16));
        return Materialize(leaf, key);
    }

    /// <summary>
    /// 自簽用戶端憑證（issuer == subject，digitalSignature + clientAuth）。
    /// 授權伺服器以此憑證的公開部分登錄為 Client 的 JWKS，即 OpenIddict self_signed_tls_client_auth。
    /// </summary>
    public static X509Certificate2 CreateSelfSignedClientCertificate(string commonName)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ClientAuthOid], true));

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return Materialize(certificate, key);
    }

    /// <summary>由指定根 CA 簽發的用戶端憑證（非自簽），用於驗證「非登錄方式」被拒絕。</summary>
    public static X509Certificate2 IssueCaSignedClientCertificate(X509Certificate2 authority, string commonName)
    {
        using var authorityKey = authority.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("CA certificate must contain a private key.");
        using var key = RSA.Create(2048);

        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ClientAuthOid], true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(authority, includeKeyIdentifier: true, includeIssuerAndSerial: false));

        var signer = X509SignatureGenerator.CreateForRSA(authorityKey, RSASignaturePadding.Pkcs1);
        var leaf = request.Create(authority.SubjectName, signer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30), RandomNumberGenerator.GetBytes(16));
        return Materialize(leaf, key);
    }

    /// <summary>取得不含私鑰的公開部分，供授權伺服器信任錨點與 Client 公開金鑰登錄使用。</summary>
    public static X509Certificate2 PublicPart(X509Certificate2 certificate)
        => X509CertificateLoader.LoadCertificate(certificate.RawData);

    /// <summary>將憑證與私鑰合併為可供 Kestrel / HttpClient 使用的 ephemeral 憑證。</summary>
    private static X509Certificate2 Materialize(X509Certificate2 certificate, RSA key)
    {
        using var withKey = certificate.HasPrivateKey ? certificate : certificate.CopyWithPrivateKey(key);
        const string password = "spike";
        return X509CertificateLoader.LoadPkcs12(withKey.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.EphemeralKeySet);
    }
}
