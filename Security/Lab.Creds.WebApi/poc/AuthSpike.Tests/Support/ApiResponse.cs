using System.Net;

namespace AuthSpike.Tests.Support;

/// <summary>HTTP 回應的狀態碼與 Body；Replayed 表示回應帶有 Idempotent-Replayed 標頭。</summary>
public sealed record ApiResponse(HttpStatusCode Status, string Body)
{
    public bool Replayed { get; init; }
}
