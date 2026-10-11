using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AuthSpike.Registration;

/// <summary>已驗證的申請者身分（17 單）：由出示的現有已核准 mTLS 憑證判定，待核准紀錄帶此身分供管理員核對。</summary>
public sealed record ApplicantIdentity(string ClientId, string CertificateThumbprint);

/// <summary>
/// 申請者持有證明（17 單，lab 暫定、待使用者確認）：申請者以新憑證的私鑰（RSA PKCS#1 v1.5 SHA-256）
/// 或新簽章金鑰的私鑰（ECDSA P-256 SHA-256）對伺服器重建的申請內容簽章，以 Base64 放入 possessionProof。
/// 申請內容由伺服器依申請欄位重建，不採用請求自行提供的內容；證明只證明持有私鑰，不取代 mTLS 的申請者驗證。
/// </summary>
public static class ApplicantProof
{
    /// <summary>憑證申請的簽章內容：綁定申請者的 Client 與新憑證指紋。</summary>
    public static string CertificateContent(string clientId, string thumbprint) => $"client-certificate-request\n{clientId}\n{thumbprint}";

    /// <summary>簽章金鑰申請的簽章內容：綁定 Client、keyid 與公開金鑰指紋。</summary>
    public static string SigningKeyContent(string clientId, string keyId, ECDsa key)
        => $"signing-key-request\n{clientId}\n{keyId}\n{Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()))}";

    /// <summary>驗證憑證申請的持有證明；以申請中的新憑證公開金鑰驗證。</summary>
    public static bool VerifyCertificate(X509Certificate2 certificate, string content, string? proof)
        => VerifyProof(proof, signature =>
        {
            var data = Encoding.UTF8.GetBytes(content);
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa is not null)
            {
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            using var ecdsa = certificate.GetECDsaPublicKey();
            return ecdsa?.VerifyData(data, signature, HashAlgorithmName.SHA256) ?? false;
        });

    /// <summary>驗證簽章金鑰申請的持有證明；以申請中的公開金鑰驗證。</summary>
    public static bool VerifySigningKey(ECDsa key, string content, string? proof)
        => VerifyProof(proof, signature => key.VerifyData(Encoding.UTF8.GetBytes(content), signature, HashAlgorithmName.SHA256));

    /// <summary>解碼並驗證持有證明；格式錯誤或密碼學例外一律視為驗證失敗。</summary>
    private static bool VerifyProof(string? proof, Func<byte[], bool> verify)
    {
        var signature = DecodeProof(proof);
        if (signature is null)
        {
            return false;
        }

        try
        {
            return verify(signature);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static byte[]? DecodeProof(string? proof)
    {
        if (string.IsNullOrWhiteSpace(proof))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(proof);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
