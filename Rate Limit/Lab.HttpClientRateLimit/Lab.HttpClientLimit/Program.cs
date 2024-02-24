using System.Threading.RateLimiting;
using Lab.HttpClientLimit;

var tokenBucketRateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
{
    TokenLimit = 1000,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 1000,
    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
    TokensPerPeriod = 17,
    AutoReplenishment = true
});
var slidingWindowRateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
{
    Window = TimeSpan.FromSeconds(10),
    SegmentsPerWindow = 100,
    AutoReplenishment = true,
    PermitLimit = 10,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 1
});
var fixedWindowRateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
{
    Window = TimeSpan.FromSeconds(10),
    AutoReplenishment = true,
    PermitLimit = 10,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 1
});

// Create an HTTP client with the client-side rate limited handler.
var limiter = fixedWindowRateLimiter;

using HttpClient client = new(
    handler: new ClientSideRateLimitedHandler(limiter: limiter));

Console.WriteLine($"{DateTime.Now.ToString()},Start");

var count = 0;
while (false)
{
    var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken: default);
    if (lease.IsAcquired == false)
    {
        Console.WriteLine("Rate limit exceeded. Pausing requests for 1 minute.");
        await Task.Delay(TimeSpan.FromMinutes(1));
        continue;
    }

    var tasks = new List<Task>()
    {
        Task.Run(async () => { await Task.Delay(TimeSpan.FromMilliseconds(100)); }),
        Task.Run(async () => { await Task.Delay(TimeSpan.FromMilliseconds(120)); }),
    };
    await Task.WhenAll(tasks);
    count++;
    Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}, Run Count: {count}");
}

var oneHundredUrls = Enumerable.Range(0, 100).Select(
    i => $"https://example.com?iteration={i:0#}");

// Flood the HTTP client with requests.
var floodOneThroughFortyNineTask = Parallel.ForEachAsync(
    source: oneHundredUrls.Take(0..49),
    body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));

var floodFiftyThroughOneHundredTask = Parallel.ForEachAsync(
    source: oneHundredUrls.Take(^50..),
    body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));

await Task.WhenAll(
    floodOneThroughFortyNineTask,
    floodFiftyThroughOneHundredTask);

static async ValueTask GetAsync(
    HttpClient client, string url, CancellationToken cancellationToken)
{
    using var response =
        await client.GetAsync(url, cancellationToken);

    Console.WriteLine(
        $"URL: {url}, HTTP status code: {response.StatusCode} ({(int)response.StatusCode})");
}