using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AuthSpike.Hosting;
using AuthSpike.Signing;

namespace AuthSpike.Tests.Support;

/// <summary>各 Steps 共用的「建立簽章請求並送出」實作，避免每個 Steps 檔重複一份。</summary>
public static class SignedHttp
{
    public static string NewNonce() => Guid.NewGuid().ToString("N");

    /// <summary>每次呼叫都建立新連線，確保 TLS 用戶端憑證依本次呼叫決定；certificate 為 null 表示不出示用戶端憑證。</summary>
    public static HttpClient CreateClient(SpikeRuntime runtime, X509Certificate2? certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        if (certificate is not null)
        {
            handler.ClientCertificates.Add(certificate);
        }

        return new HttpClient(handler, disposeHandler: true);
    }

    /// <summary>以現在時間簽署（有效 60 秒、隨機 nonce）。</summary>
    public static Task SignNowAsync(HttpRequestMessage request, SignatureKey key)
    {
        var now = DateTimeOffset.UtcNow;
        return BusinessRequestSigner.SignAsync(request, key, now, now.AddSeconds(60), NewNonce());
    }

    /// <summary>建立帶 Bearer Token 的請求並簽章；body 不為 null 時以 JSON 送出，idempotencyKey 不為 null 時加上 Idempotency-Key。</summary>
    public static async Task<HttpRequestMessage> BuildSignedAsync(
        HttpMethod method,
        Uri uri,
        string? token,
        SignatureKey key,
        string? body = null,
        string? idempotencyKey = null,
        DateTimeOffset? created = null,
        DateTimeOffset? expires = null,
        string? nonce = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        var signedAt = created ?? DateTimeOffset.UtcNow;
        await BusinessRequestSigner.SignAsync(request, key, signedAt, expires ?? signedAt.AddSeconds(60), nonce ?? NewNonce());
        return request;
    }

    /// <summary>複製已簽章請求並改送往 port；port 與主要執行個體不同時沿用主要執行個體的公開目標（Host），模擬經同一入口的多個執行個體。</summary>
    public static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source, int port, int primaryPort)
    {
        var clone = new HttpRequestMessage(source.Method, new UriBuilder(source.RequestUri!) { Port = port }.Uri);
        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (port != primaryPort)
        {
            clone.Headers.Host = $"localhost:{primaryPort}";
        }

        if (source.Content is not null)
        {
            clone.Content = new ByteArrayContent(await source.Content.ReadAsByteArrayAsync());
            foreach (var header in source.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    public static async Task<ApiResponse> SendAsync(HttpClient client, HttpRequestMessage request)
    {
        using var response = await client.SendAsync(request);
        var replayed = response.Headers.TryGetValues("Idempotent-Replayed", out var values) && values.Contains("true");
        return new ApiResponse(response.StatusCode, await response.Content.ReadAsStringAsync()) { Replayed = replayed };
    }
}
