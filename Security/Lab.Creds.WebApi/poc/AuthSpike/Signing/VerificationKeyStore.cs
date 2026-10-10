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

    /// <summary>指定 Client 已登錄的 keyId（含重疊期與已退役者）；供管理員退役時確認是否仍有可用的替代金鑰（14 單）。</summary>
    public IReadOnlyList<string> KeyIdsOf(string clientId)
    {
        lock (_gate)
        {
            return _keysByClient.TryGetValue(clientId, out var keys)
                ? keys.Select(candidate => candidate.KeyId).ToList()
                : [];
        }
    }

    /// <summary>keyId 是否也登錄於其他 Client；簽章金鑰的撤銷與退役以 keyId 為鍵，共用時會波及其他 Client（14 單）。</summary>
    public bool IsKeyIdSharedWithOtherClient(string clientId, string keyId)
    {
        lock (_gate)
        {
            return _keysByClient.Any(pair => pair.Key != clientId && pair.Value.Any(candidate => candidate.KeyId == keyId));
        }
    }
}
