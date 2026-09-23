using System.Net;

namespace Lab.Signature.WebApi.Tests.Support;

/// <summary>
/// 每個 Scenario 各自獨立的一份實例（由 Reqnroll 的內建 DI 每個 Scenario 各建立一次），
/// 用來在 Given/When/Then step 之間傳遞狀態。
/// </summary>
public class SignatureScenarioContext
{
    // 主要 / 次要 ApiKey 憑證（次要用於「同一 Nonce 不同 ApiKey 互不影響」情境）
    public string ApiKey { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string SecondaryApiKey { get; set; } = string.Empty;
    public string SecondarySecret { get; set; } = string.Empty;

    // 請求內容 / 覆寫參數
    public string BodyText { get; set; } = string.Empty;
    public string? TamperedBodyText { get; set; }
    public string? TimestampOverride { get; set; }
    public string? NonceOverride { get; set; }
    public string? QueryString { get; set; }
    public HashSet<string> HeadersToOmit { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Func<string, string>? SignatureTransform { get; set; }
    public string? SignatureOverride { get; set; }

    // 上一次送出的請求資訊，供「重送同一 Nonce」等情境沿用
    public string? LastMethod { get; set; }
    public string? LastBasePath { get; set; }
    public string? LastTimestamp { get; set; }
    public string? LastNonce { get; set; }
    public string? LastSignature { get; set; }
    public string? LastCanonicalString { get; set; }

    // 最近一次的回應
    public HttpResponseMessage? LastResponse { get; set; }
    public string LastResponseBody { get; set; } = string.Empty;

    // 併發 Nonce 測試專用
    public ConcurrentCallResult[]? ConcurrentResults { get; set; }
}

public sealed record ConcurrentCallResult(HttpStatusCode StatusCode, string Body);
