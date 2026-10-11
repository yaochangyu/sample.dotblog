using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AuthSpike.Hosting;
using AuthSpike.Registration;
using AuthSpike.Signing;

namespace AuthSpike.Tests.Support;

/// <summary>持有證明：簽章與同次簽署的時間戳、一次性 nonce。Timestamp 或 Nonce 為 null 時表示該欄位不送出（用於缺漏情境）。</summary>
public sealed record PossessionProof(string? Signature, long? Timestamp, string? Nonce)
{
    /// <summary>移除時間戳與 nonce，模擬只送簽章的舊格式申請。</summary>
    public PossessionProof WithoutTiming() => this with { Timestamp = null, Nonce = null };
}

/// <summary>
/// 登錄申請的測試輔助（17、18、21 單）：以真實私鑰產生持有證明，並以申請者的 mTLS 憑證送出 HTTP 申請。
/// 簽章內容與格式由 AuthSpike 的 ApplicantProof 定義，測試不另外發明格式。
/// 預設申請者為 clientId 目前登錄的憑證；需要模擬其他申請者身分時，使用帶 applicantThumbprint 的覆寫。
/// </summary>
public static class ApplicantProofs
{
    /// <summary>以新憑證的私鑰對申請內容簽章（預設申請者為 clientId 的登錄憑證，時間為現在，nonce 為新值）。</summary>
    public static PossessionProof CertificateProof(string clientId, X509Certificate2 newCertificate)
        => CertificateProofAt(clientId, SpikeEnvironment.Runtime.ClientCertificate(clientId).Thumbprint, newCertificate, newCertificate, NowUnix(), NewNonce());

    /// <summary>以他人（非新憑證）的私鑰對新憑證的申請內容簽章，模擬證明失敗（17 單）。</summary>
    public static PossessionProof ForeignCertificateProof(string clientId, X509Certificate2 newCertificate, X509Certificate2 foreignCertificate)
        => CertificateProofAt(clientId, SpikeEnvironment.Runtime.ClientCertificate(clientId).Thumbprint, newCertificate, foreignCertificate, NowUnix(), NewNonce());

    /// <summary>
    /// 以指定的申請者指紋、時間戳與 nonce 產生憑證申請的持有證明；signer 為新憑證本身（正常證明）或他人（失敗證明）。
    /// 申請者指紋不是出示的 mTLS 憑證時，證明與申請者身分不符（18 單後續）。
    /// </summary>
    public static PossessionProof CertificateProofAt(string clientId, string applicantThumbprint, X509Certificate2 newCertificate, X509Certificate2 signer, long timestamp, string nonce)
    {
        using var rsa = signer.GetRSAPrivateKey() ?? throw new InvalidOperationException("簽署持有證明的憑證必須含私鑰。");
        var content = Encoding.UTF8.GetBytes(ApplicantProof.CertificateContent(clientId, applicantThumbprint, newCertificate.Thumbprint, timestamp, nonce));
        var signature = Convert.ToBase64String(rsa.SignData(content, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return new PossessionProof(signature, timestamp, nonce);
    }

    /// <summary>以新簽章金鑰的私鑰對申請內容簽章（ECDSA SHA-256），預設申請者為 clientId 的登錄憑證。</summary>
    public static PossessionProof SigningKeyProof(string clientId, string keyId, ECDsa key)
        => SigningKeyProofAt(clientId, SpikeEnvironment.Runtime.ClientCertificate(clientId).Thumbprint, keyId, key, key, NowUnix(), NewNonce());

    /// <summary>以指定的申請者指紋、時間戳與 nonce 產生簽章金鑰申請的持有證明；signer 為申請中的金鑰（正常）或他人金鑰（失敗）。</summary>
    public static PossessionProof SigningKeyProofAt(string clientId, string applicantThumbprint, string keyId, ECDsa key, ECDsa signer, long timestamp, string nonce)
    {
        var content = Encoding.UTF8.GetBytes(ApplicantProof.SigningKeyContent(clientId, applicantThumbprint, keyId, key, timestamp, nonce));
        var signature = Convert.ToBase64String(signer.SignData(content, HashAlgorithmName.SHA256));
        return new PossessionProof(signature, timestamp, nonce);
    }

    public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string NewNonce() => Guid.NewGuid().ToString("N");

    /// <summary>以指定的 mTLS 身分（null 表示無憑證）送出憑證登錄申請。</summary>
    public static Task<ApiResponse> SubmitCertificateAsync(SpikeRuntime runtime, X509Certificate2? identity, string clientId, string publicCertificatePem, PossessionProof? proof)
        => PostAsync(runtime, identity, "client-certificate-requests", new
        {
            clientId,
            publicCertificatePem,
            possessionProof = proof?.Signature,
            proofTimestamp = proof?.Timestamp,
            proofNonce = proof?.Nonce,
        });

    /// <summary>以指定的 mTLS 身分（null 表示無憑證）送出簽章金鑰登錄申請。</summary>
    public static Task<ApiResponse> SubmitSigningKeyAsync(SpikeRuntime runtime, X509Certificate2? identity, string clientId, string keyId, string publicKeyPem, PossessionProof? proof)
        => PostAsync(runtime, identity, "signing-key-requests", new
        {
            clientId,
            keyId,
            publicKeyPem,
            possessionProof = proof?.Signature,
            proofTimestamp = proof?.Timestamp,
            proofNonce = proof?.Nonce,
        });

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
