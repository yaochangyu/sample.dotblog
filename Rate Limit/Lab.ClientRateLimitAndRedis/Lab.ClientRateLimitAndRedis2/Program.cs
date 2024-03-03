using Lab.ClientRateLimitAndRedis2;
using RedisRateLimiting;
using StackExchange.Redis;

var redisTokenBucketRateLimiter = new RedisTokenBucketRateLimiter<string>("demo-redis-token-bucket",
    new RedisTokenBucketRateLimiterOptions
    {
        ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect("localhost"),
        TokenLimit = 50,
        TokensPerPeriod = 50,
        ReplenishmentPeriod = TimeSpan.FromSeconds(10) 
    }
);

var redisSlidingWindowRateLimiter = new RedisSlidingWindowRateLimiter<string>("demo-redis-sliding-window",
    new RedisSlidingWindowRateLimiterOptions
    {
        ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect("localhost"),
        Window = TimeSpan.FromSeconds(10),
        PermitLimit = 50
    }
);
var redisFixedWindowRateLimiter = new RedisFixedWindowRateLimiter<string>("demo-redis-fixed-window",
    new RedisFixedWindowRateLimiterOptions
    {
        ConnectionMultiplexerFactory = () => ConnectionMultiplexer.Connect("localhost"),
        Window = TimeSpan.FromSeconds(10),
        PermitLimit = 50
    }
);

// Create an HTTP client with the client-side rate limited handler.
var limiter = redisSlidingWindowRateLimiter;

using HttpClient client = new(
    handler: new ClientSideRateLimitedHandler(limiter: limiter));

Console.WriteLine($"{DateTime.Now.ToString()},Start");

var count = 0;
while (true)
{
    var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken: default);
    if (lease.IsAcquired == false)
    {
        Console.WriteLine("Rate limit exceeded. Pausing requests for 1 sec.");
        await Task.Delay(TimeSpan.FromSeconds(1));
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