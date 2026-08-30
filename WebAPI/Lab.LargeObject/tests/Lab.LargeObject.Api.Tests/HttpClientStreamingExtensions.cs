using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Lab.LargeObject.Api.Tests;

/// <summary>
/// HttpClient 串流接收擴充方法（達成 Client 端 0 LOH 逐筆消費）
/// </summary>
public static class HttpClientStreamingExtensions
{
    public static async IAsyncEnumerable<T> GetFromJsonStreamingAsync<T>(
        this HttpClient client,
        string requestUri,
        JsonSerializerOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // 關鍵 1：必須使用 ResponseHeadersRead，避免 HttpClient 內部緩衝整個 Body
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // 關鍵 2：取得底層 Stream，以 DeserializeAsyncEnumerable 逐筆串流解析
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
            stream,
            options,
            cancellationToken: cancellationToken).WithCancellation(cancellationToken))
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }
}
