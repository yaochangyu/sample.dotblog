namespace AuthSpike.Signing;

/// <summary>
/// 業務 API 驗簽用的簽章金鑰登錄（只含公開部分），依 Client 分組，可同時登錄新舊金鑰（重疊期）。
/// 主要與第二個執行個體共用同一份登錄；查詢一律限定於已驗證 Client 自己的金鑰（08 單，混用其他 Client 的金鑰一律找不到）。
/// </summary>
public sealed class VerificationKeyStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, List<SignatureKey>> _keysByClient = new();

    public void Register(string clientId, SignatureKey publicKey)
    {
        lock (_gate)
        {
            if (!_keysByClient.TryGetValue(clientId, out var keys))
            {
                keys = [];
                _keysByClient[clientId] = keys;
            }

            keys.Add(publicKey);
        }
    }

    /// <summary>只在指定 Client 登錄的金鑰中查找；keyId 屬於其他 Client 時找不到。</summary>
    public bool TryGet(string clientId, string keyId, out SignatureKey? key)
    {
        lock (_gate)
        {
            key = _keysByClient.TryGetValue(clientId, out var keys)
                ? keys.FirstOrDefault(candidate => candidate.KeyId == keyId)
                : null;
            return key is not null;
        }
    }
}
