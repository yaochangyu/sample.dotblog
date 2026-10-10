using System.Collections.Concurrent;

namespace AuthSpike.Trust;

/// <summary>
/// 撤銷狀態的權威來源（lab：同一程序內共用的記憶體狀態，撤銷即時寫入，同步延遲為 0）。
/// 授權伺服器的 Token 端點與業務 API 的接受判斷都讀取此狀態。
/// </summary>
public sealed class TrustRegistry
{
    private readonly ConcurrentDictionary<string, byte> _disabledClients = new();
    private readonly ConcurrentDictionary<string, byte> _revokedCertificates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _revokedSigningKeys = new();

    public void DisableClient(string clientId) => _disabledClients[clientId] = 0;

    public bool IsClientEnabled(string clientId) => !_disabledClients.ContainsKey(clientId);

    public void RevokeCertificate(string thumbprint) => _revokedCertificates[thumbprint] = 0;

    public bool IsCertificateRevoked(string thumbprint) => _revokedCertificates.ContainsKey(thumbprint);

    public void RevokeSigningKey(string keyId) => _revokedSigningKeys[keyId] = 0;

    public bool IsSigningKeyRevoked(string keyId) => _revokedSigningKeys.ContainsKey(keyId);
}
