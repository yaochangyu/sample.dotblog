using System.Security.Cryptography;
using AuthSpike.Signing;

namespace AuthSpike.Registration;

/// <summary>簽章金鑰登錄申請：只含 Client 身分、keyid 與公開金鑰，不含私鑰；與 mTLS 憑證申請分開管理與核准（13 單）。</summary>
public sealed record SigningKeyRegistrationRequest(Guid RequestId, string ClientId, SignatureKey PublicKey, CertificateRequestStatus Status);

/// <summary>
/// 簽章金鑰登錄申請的程序內儲存（lab：Dictionary 加 lock）。申請不會改變驗簽金鑰登錄；只有管理員核准才會由 AuthServerHost 把金鑰登錄到該申請的 Client 名下。
/// 核准簽章金鑰不使該 Client 的 mTLS 憑證生效，反之亦然。
/// </summary>
public sealed class SigningKeyRegistrationRequests
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, SigningKeyRegistrationRequest> _requests = new();

    public SigningKeyRegistrationRequest Submit(string clientId, SignatureKey publicKey)
    {
        lock (_gate)
        {
            var request = new SigningKeyRegistrationRequest(Guid.NewGuid(), clientId, publicKey, CertificateRequestStatus.Pending);
            _requests[request.RequestId] = request;
            return request;
        }
    }

    public SigningKeyRegistrationRequest? Find(Guid requestId)
    {
        lock (_gate)
        {
            return _requests.GetValueOrDefault(requestId);
        }
    }

    /// <summary>更新申請狀態；呼叫端須先確認申請為待核准（AuthServerHost 以決策閘門序列化核准與拒絕）。</summary>
    public void Decide(Guid requestId, CertificateRequestStatus decision)
    {
        lock (_gate)
        {
            var existing = _requests[requestId];
            _requests[requestId] = existing with { Status = decision };
        }
    }

    /// <summary>
    /// 解析申請的公開金鑰。只接受 ECDSA P-256 的公開金鑰（SubjectPublicKeyInfo PEM）；含私鑰的內容一律拒絕，不解析、不儲存。
    /// </summary>
    public static bool TryParsePublicKey(string? keyId, string? publicKeyPem, out SignatureKey? key, out string error)
    {
        key = null;
        error = "invalid_public_key";
        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            return false;
        }

        if (publicKeyPem.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            error = "private_key_not_allowed";
            return false;
        }

        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(publicKeyPem);
            if (ecdsa.ExportParameters(includePrivateParameters: false).Curve.Oid?.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                ecdsa.Dispose();
                return false;
            }

            key = new SignatureKey(keyId, ecdsa);
            return true;
        }
        catch (CryptographicException)
        {
            ecdsa.Dispose();
            return false;
        }
    }
}
