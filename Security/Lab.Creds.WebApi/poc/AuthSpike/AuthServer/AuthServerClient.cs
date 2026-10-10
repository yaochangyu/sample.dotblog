using System.Security.Cryptography.X509Certificates;

namespace AuthSpike.AuthServer;

/// <summary>要登錄到授權伺服器的服務 Client、其自簽用戶端憑證的公開部分、核准的目標 API（audience）與核准的 scope 白名單。</summary>
public sealed record AuthServerClient(string ClientId, X509Certificate2 PublicCertificate, string Audience, IReadOnlyList<string> Scopes);
