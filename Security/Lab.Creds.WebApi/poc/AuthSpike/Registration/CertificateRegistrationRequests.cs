using System.Security.Cryptography.X509Certificates;
using AuthSpike.Certificates;

namespace AuthSpike.Registration;

/// <summary>憑證登錄申請的狀態（12 單）：只有待核准的申請可被核准或拒絕，已核准或已拒絕的申請不可再改變。</summary>
public enum CertificateRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>登錄申請：只含 Client 身分與公開憑證，不含私鑰。</summary>
public sealed record CertificateRegistrationRequest(Guid RequestId, string ClientId, X509Certificate2 PublicCertificate, CertificateRequestStatus Status);

/// <summary>
/// 憑證登錄申請的程序內儲存（lab：Dictionary 加 lock）。申請以 requestId 識別，各申請彼此獨立，互不影響其他 Client 的信任狀態。
/// 申請不會改變信任名單；只有管理員核准才會由 AuthServerHost 把憑證加入信任名單。
/// </summary>
public sealed class CertificateRegistrationRequests
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CertificateRegistrationRequest> _requests = new();

    public CertificateRegistrationRequest Submit(string clientId, X509Certificate2 publicCertificate)
    {
        lock (_gate)
        {
            var request = new CertificateRegistrationRequest(Guid.NewGuid(), clientId, publicCertificate, CertificateRequestStatus.Pending);
            _requests[request.RequestId] = request;
            return request;
        }
    }

    public CertificateRegistrationRequest? Find(Guid requestId)
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
    /// 解析申請的公開憑證。只接受不含私鑰的 PEM 憑證；含私鑰的內容一律拒絕，不解析、不儲存。
    /// </summary>
    public static bool TryParsePublicCertificate(string? pem, out X509Certificate2? certificate, out string error)
    {
        certificate = null;
        error = "invalid_certificate";
        if (string.IsNullOrWhiteSpace(pem))
        {
            return false;
        }

        if (pem.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            error = "private_key_not_allowed";
            return false;
        }

        try
        {
            using var parsed = X509Certificate2.CreateFromPem(pem);
            if (parsed.HasPrivateKey)
            {
                error = "private_key_not_allowed";
                return false;
            }

            certificate = SpikeCertificates.PublicPart(parsed);
            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    public static string ToWire(CertificateRequestStatus status) => status switch
    {
        CertificateRequestStatus.Pending => "pending",
        CertificateRequestStatus.Approved => "approved",
        CertificateRequestStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "未知的申請狀態"),
    };
}
