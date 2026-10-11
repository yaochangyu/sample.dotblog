namespace AuthSpike.Registration;

/// <summary>
/// 持有證明 nonce 的一次性登錄（19 單後續，lab 暫定）：已受理的證明其 nonce 只能使用一次。
/// 程序內儲存，只在單程序內有效；每筆保存至該證明的時間窗結束為止，之後該證明本身已因時間窗失效。
/// 呼叫端須在決策閘門內呼叫，檢查與登錄的順序由閘門序列化。
/// </summary>
public sealed class ProofNonceStore
{
    private readonly Dictionary<string, long> _expiresAt = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>nonce 是否已被使用（仍在保存期內）。</summary>
    public bool IsConsumed(string nonce, DateTimeOffset now)
    {
        lock (_gate)
        {
            Prune(now.ToUnixTimeSeconds());
            return _expiresAt.ContainsKey(nonce);
        }
    }

    /// <summary>登錄已受理證明的 nonce；保存至 timestamp 加上時間窗。</summary>
    public void Consume(string nonce, long timestamp)
    {
        lock (_gate)
        {
            _expiresAt[nonce] = timestamp + ApplicantProof.FreshnessWindowSeconds;
        }
    }

    private void Prune(long nowSeconds)
    {
        foreach (var expired in _expiresAt.Where(pair => pair.Value < nowSeconds).Select(pair => pair.Key).ToList())
        {
            _expiresAt.Remove(expired);
        }
    }
}
