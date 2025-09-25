using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using System.Net.Http;

[MemoryDiagnoser]
[SimpleJob]
public class FixedHttpClientBenchmark
{
    private HttpClient _resilienceClient = null!;
    private HttpClient _pollyV8Client = null!;
    private HttpClient _pollyV7Client = null!;
    private HttpClient _standardClient = null!;
    private readonly string _baseUrl = "http://localhost:5068";

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // 設定標準 HTTP 客戶端（基準）
        services.AddHttpClient("standard", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        });

        // 設定 Resilience 客戶端 (Microsoft.Extensions.Http.Resilience)
        services.AddHttpClient("resilience", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        })
        .AddStandardResilienceHandler();

        // 設定 Polly V8 客戶端 (使用新的 ResiliencePipelineBuilder)
        services.AddHttpClient("pollyV8", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        })
        .AddResilienceHandler("retry", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => !response.IsSuccessStatusCode)
                    .Handle<HttpRequestException>()
            });
        });

        // 設定 Polly V7 客戶端 (傳統方式，使用 Microsoft.Extensions.Http.Polly)
        services.AddHttpClient("pollyV7", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        })
        .AddPolicyHandler(GetRetryPolicyV7());

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        _standardClient = httpClientFactory.CreateClient("standard");
        _resilienceClient = httpClientFactory.CreateClient("resilience");
        _pollyV8Client = httpClientFactory.CreateClient("pollyV8");
        _pollyV7Client = httpClientFactory.CreateClient("pollyV7");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _standardClient?.Dispose();
        _resilienceClient?.Dispose();
        _pollyV8Client?.Dispose();
        _pollyV7Client?.Dispose();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicyV7()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    [Benchmark(Baseline = true)]
    public async Task<string> StandardHttpClient()
    {
        try
        {
            var response = await _standardClient.GetAsync("/api/members");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [Benchmark]
    public async Task<string> ResilienceHttpClient()
    {
        try
        {
            var response = await _resilienceClient.GetAsync("/api/members");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [Benchmark]
    public async Task<string> PollyV8HttpClient()
    {
        try
        {
            var response = await _pollyV8Client.GetAsync("/api/members");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [Benchmark]
    public async Task<string> PollyV7HttpClient()
    {
        try
        {
            var response = await _pollyV7Client.GetAsync("/api/members");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}