using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

[MemoryDiagnoser]
[SimpleJob]
public class HttpClientBenchmark
{
    private HttpClient _resilienceClient = null!;
    private HttpClient _pollyClient = null!;
    private HttpClient _standardClient = null!;
    private readonly string _baseUrl = "http://localhost:5068";

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // 設定 Resilience 客戶端
        services.AddHttpClient("resilience", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        })
        .AddStandardResilienceHandler();

        // 設定 Polly 客戶端
        services.AddHttpClient("polly", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // 設定標準 HTTP 客戶端（作為基準）
        services.AddHttpClient("standard", client =>
        {
            client.BaseAddress = new Uri(_baseUrl);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        _resilienceClient = httpClientFactory.CreateClient("resilience");
        _pollyClient = httpClientFactory.CreateClient("polly");
        _standardClient = httpClientFactory.CreateClient("standard");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _resilienceClient?.Dispose();
        _pollyClient?.Dispose();
        _standardClient?.Dispose();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Polly Retry {retryCount} after {timespan} seconds");
                });
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
    public async Task<string> PollyHttpClient()
    {
        try
        {
            var response = await _pollyClient.GetAsync("/api/members");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}