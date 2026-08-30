using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lab.LargeObject.Api;

var mode = args.FirstOrDefault(a => a.StartsWith("--mode="))?.Split('=')[1] ?? "stream";
var type = args.FirstOrDefault(a => a.StartsWith("--type="))?.Split('=')[1] ?? "members";
var baseUrl = args.FirstOrDefault(a => a.StartsWith("--url="))?.Split('=')[1] ?? "http://localhost:5148";
var requests = int.Parse(args.FirstOrDefault(a => a.StartsWith("--requests="))?.Split('=')[1] ?? "50");
var concurrency = int.Parse(args.FirstOrDefault(a => a.StartsWith("--concurrency="))?.Split('=')[1] ?? "10");

var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = concurrency * 2
};
using var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

// 暖機以消除 JIT 與初期 Socket 連線開銷
try
{
    await client.GetAsync("/");
}
catch { }

// 強制執行一次完整 GC 回到基線
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var initialAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);
var initialPauseDuration = GC.GetTotalPauseDuration();
var initialGen0 = GC.CollectionCount(0);
var initialGen1 = GC.CollectionCount(1);
var initialGen2 = GC.CollectionCount(2);

var endpoint = (type, mode) switch
{
    ("readings", "list") => "/api/export-readings-list",
    ("readings", "stream") => "/api/export-readings-stream",
    ("strings", "list") => "/api/export-strings-list",
    ("strings", "stream") => "/api/export-strings-stream",
    ("members", "list") => "/api/export-members-list",
    ("members", "stream") => "/api/export-members-stream",
    ("members-class", "list") => "/api/export-members-class-list",
    ("members-class", "stream") => "/api/export-members-class-stream",
    _ => "/api/export-members-stream"
};

var sw = Stopwatch.StartNew();

using var semaphore = new SemaphoreSlim(concurrency);
var tasks = Enumerable.Range(0, requests).Select(async _ =>
{
    await semaphore.WaitAsync();
    try
    {
        if (mode == "list")
        {
            // ❌ 未池化一次性接收：在 Client 記憶體建立大 List
            if (type == "readings")
            {
                var list = await client.GetFromJsonAsync<List<double>>(endpoint);
                Consume(list?.Count);
            }
            else if (type == "strings")
            {
                var list = await client.GetFromJsonAsync<List<string>>(endpoint);
                Consume(list?.Count);
            }
            else if (type == "members")
            {
                var list = await client.GetFromJsonAsync<List<MemberAccount>>(endpoint, jsonOptions);
                Consume(list?.Count);
            }
            else if (type == "members-class")
            {
                var list = await client.GetFromJsonAsync<List<MemberAccountClass>>(endpoint, jsonOptions);
                Consume(list?.Count);
            }
        }
        else
        {
            // 🏆 串流接收：ResponseHeadersRead + DeserializeAsyncEnumerable 逐筆消費
            if (type == "readings")
            {
                await foreach (var item in GetStreamAsync<double>(client, endpoint, null))
                {
                    Consume(item);
                }
            }
            else if (type == "strings")
            {
                await foreach (var item in GetStreamAsync<string>(client, endpoint, null))
                {
                    Consume(item?.Length);
                }
            }
            else if (type == "members")
            {
                await foreach (var item in GetStreamAsync<MemberAccount>(client, endpoint, jsonOptions))
                {
                    Consume(item.MemberId);
                }
            }
            else if (type == "members-class")
            {
                await foreach (var item in GetStreamAsync<MemberAccountClass>(client, endpoint, jsonOptions))
                {
                    Consume(item.MemberId);
                }
            }
        }
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);
sw.Stop();

var finalAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);
var finalPauseDuration = GC.GetTotalPauseDuration();
var finalGen0 = GC.CollectionCount(0);
var finalGen1 = GC.CollectionCount(1);
var finalGen2 = GC.CollectionCount(2);

var memInfo = GC.GetGCMemoryInfo();
var lohSize = memInfo.GenerationInfo.Length > 3 ? memInfo.GenerationInfo[3].SizeAfterBytes : 0;
var totalAllocatedMb = (finalAllocatedBytes - initialAllocatedBytes) / 1024.0 / 1024.0;
var pauseDurationMs = (finalPauseDuration - initialPauseDuration).TotalMilliseconds;

var result = new
{
    Mode = mode,
    Type = type,
    ElapsedMs = sw.ElapsedMilliseconds,
    TotalAllocatedMb = Math.Round(totalAllocatedMb, 2),
    PauseDurationMs = Math.Round(pauseDurationMs, 2),
    PauseTimePercentage = memInfo.PauseTimePercentage,
    Gen0 = finalGen0 - initialGen0,
    Gen1 = finalGen1 - initialGen1,
    Gen2 = finalGen2 - initialGen2,
    LohSizeMb = Math.Round(lohSize / 1024.0 / 1024.0, 2)
};

Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));

static async IAsyncEnumerable<T> GetStreamAsync<T>(HttpClient client, string url, JsonSerializerOptions? options, [EnumeratorCancellation] CancellationToken ct = default)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync(ct);
    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken: ct))
    {
        if (item is not null)
        {
            yield return item;
        }
    }
}

static void Consume(object? val)
{
    // 避免編譯器優化消除運算
    _ = val?.GetHashCode();
}
