using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AuthSpike.Hosting;
using AuthSpike.Registration;
using AuthSpike.Signing;

namespace AuthSpike.Tests.Support;

/// <summary>
/// 登錄申請的測試輔助（17 單）：以真實私鑰產生持有證明，並以申請者的 mTLS 憑證送出 HTTP 申請。
/// 簽章內容與格式由 AuthSpike 的 ApplicantProof 定義，測試不另外發明格式。
/// </summary>
public static class ApplicantProofs
{
    /// <summary>以新憑證的私鑰對申請內容簽章（RSA PKCS#1 SHA-256），回傳 Base64。</summary>
    public static string CertificateProof(string clientId, X509Certificate2 newCertificate)
    {
        using var rsa = newCertificate.GetRSAPrivateKey() ?? throw new InvalidOperationException("新憑證必須含私鑰才能產生持有證明。");
        var content = Encoding.UTF8.GetBytes(ApplicantProof.CertificateContent(clientId, newCertificate.Thumbprint));
        return Convert.ToBase64String(rsa.SignData(content, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    /// <summary>以他人（非新憑證）的私鑰對新憑證的申請內容簽章，模擬證明失敗（17 單）。</summary>
    public static string ForeignCertificateProof(string clientId, X509Certificate2 newCertificate, X509Certificate2 foreignCertificate)
    {
        using var rsa = foreignCertificate.GetRSAPrivateKey() ?? throw new InvalidOperationException("他人憑證必須含私鑰。");
        var content = Encoding.UTF8.GetBytes(ApplicantProof.CertificateContent(clientId, newCertificate.Thumbprint));
        return Convert.ToBase64String(rsa.SignData(content, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    /// <summary>以新簽章金鑰的私鑰對申請內容簽章（ECDSA SHA-256），回傳 Base64。</summary>
    public static string SigningKeyProof(string clientId, string keyId, ECDsa key)
    {
        var content = Encoding.UTF8.GetBytes(ApplicantProof.SigningKeyContent(clientId, keyId, key));
        return Convert.ToBase64String(key.SignData(content, HashAlgorithmName.SHA256));
    }

    /// <summary>以指定的 mTLS 身分（null 表示無憑證）送出憑證登錄申請。</summary>
    public static Task<ApiResponse> SubmitCertificateAsync(SpikeRuntime runtime, X509Certificate2? identity, string clientId, string publicCertificatePem, string? proof)
        => PostAsync(runtime, identity, "client-certificate-requests", new { clientId, publicCertificatePem, possessionProof = proof });

    /// <summary>以指定的 mTLS 身分（null 表示無憑證）送出簽章金鑰登錄申請。</summary>
    public static Task<ApiResponse> SubmitSigningKeyAsync(SpikeRuntime runtime, X509Certificate2? identity, string clientId, string keyId, string publicKeyPem, string? proof)
        => PostAsync(runtime, identity, "signing-key-requests", new { clientId, keyId, publicKeyPem, possessionProof = proof });

    private static async Task<ApiResponse> PostAsync(SpikeRuntime runtime, X509Certificate2? identity, string relativePath, object body)
    {
        using var client = SignedHttp.CreateClient(runtime, identity);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(runtime.AuthServer.Issuer, relativePath))
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        var response = await SignedHttp.SendAsync(client, request);
        if (response.Status == HttpStatusCode.Accepted)
        {
            PendingRequestCleanup.Track(runtime, relativePath, response.Body);
        }

        return response;
    }
}
