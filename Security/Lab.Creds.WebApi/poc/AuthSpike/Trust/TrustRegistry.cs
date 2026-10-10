using System.Collections.Concurrent;

namespace AuthSpike.Trust;

/// <summary>
/// 撤銷與退役狀態的權威來源（lab：同一程序內共用的記憶體狀態，寫入即時生效，同步延遲為 0）。
/// 授權伺服器的 Token 端點與業務 API 的接受判斷都讀取此狀態。
/// 退役（正常輪替結束）與撤銷（洩漏）都使對應憑證或金鑰不再被接受；兩者分開記錄以利追查。
/// </summary>
public sealed class TrustRegistry
{
    private readonly ConcurrentDictionary<string, byte> _disabledClients = new();
    private readonly ConcurrentDictionary<string, byte> _revokedCertificates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _retiredCertificates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _revokedSigningKeys = new();
    private readonly ConcurrentDictionary<string, byte> _retiredSigningKeys = new();
    private readonly ConcurrentDictionary<string, byte> _administratorCertificates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>登錄管理員專屬的 mTLS 憑證（11 單，lab 暫定：啟動時由組合根登錄）。管理員身分與 Client 身分分開，不得登錄為 Client。</summary>
    public void RegisterAdministratorCertificate(string thumbprint) => _administratorCertificates[thumbprint] = 0;

    /// <summary>出示的 mTLS 憑證是否已登錄為管理員角色；管理介面只以此判斷管理員身分。</summary>
    public bool IsAdministratorCertificate(string thumbprint) => _administratorCertificates.ContainsKey(thumbprint);

    public void DisableClient(string clientId) => _disabledClients[clientId] = 0;

    public bool IsClientEnabled(string clientId) => !_disabledClients.ContainsKey(clientId);

    public void RevokeCertificate(string thumbprint) => _revokedCertificates[thumbprint] = 0;

    public bool IsCertificateRevoked(string thumbprint) => _revokedCertificates.ContainsKey(thumbprint);

    /// <summary>正常輪替結束後退役舊憑證（08 單）；退役後的憑證不得再要求 Token 或呼叫業務 API。</summary>
    public void RetireCertificate(string thumbprint) => _retiredCertificates[thumbprint] = 0;

    public bool IsCertificateRetired(string thumbprint) => _retiredCertificates.ContainsKey(thumbprint);

    /// <summary>憑證已撤銷或已退役，皆不得被接受。</summary>
    public bool IsCertificateBlocked(string thumbprint) => IsCertificateRevoked(thumbprint) || IsCertificateRetired(thumbprint);

    public void RevokeSigningKey(string keyId) => _revokedSigningKeys[keyId] = 0;

    public bool IsSigningKeyRevoked(string keyId) => _revokedSigningKeys.ContainsKey(keyId);

    /// <summary>正常輪替結束後退役舊簽章金鑰（08 單）。</summary>
    public void RetireSigningKey(string keyId) => _retiredSigningKeys[keyId] = 0;

    public bool IsSigningKeyRetired(string keyId) => _retiredSigningKeys.ContainsKey(keyId);

    /// <summary>簽章金鑰已撤銷或已退役，皆不得被接受。</summary>
    public bool IsSigningKeyBlocked(string keyId) => IsSigningKeyRevoked(keyId) || IsSigningKeyRetired(keyId);
}
