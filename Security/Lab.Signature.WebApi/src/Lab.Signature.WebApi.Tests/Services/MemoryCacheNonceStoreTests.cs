using Lab.Signature.WebApi.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Lab.Signature.WebApi.Tests.Services;

public class MemoryCacheNonceStoreTests
{
    private static MemoryCacheNonceStore CreateStore() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task TryConsumeAsync_FirstCall_ReturnsTrue()
    {
        var store = CreateStore();

        var result = await store.TryConsumeAsync("demo-api-key-001", "nonce-abc");

        Assert.True(result);
    }

    [Fact]
    public async Task TryConsumeAsync_SameNonceReplayed_ReturnsFalseOnSecondCall()
    {
        var store = CreateStore();

        var first = await store.TryConsumeAsync("demo-api-key-001", "nonce-abc");
        var second = await store.TryConsumeAsync("demo-api-key-001", "nonce-abc");

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task TryConsumeAsync_SameNonceDifferentApiKey_AreIndependent()
    {
        var store = CreateStore();

        var resultForKeyA = await store.TryConsumeAsync("demo-api-key-001", "nonce-shared");
        var resultForKeyB = await store.TryConsumeAsync("demo-api-key-002", "nonce-shared");

        Assert.True(resultForKeyA);
        Assert.True(resultForKeyB);
    }

    [Fact]
    public async Task TryConsumeAsync_ConcurrentCallsWithSameNonce_OnlyOneSucceeds()
    {
        var store = CreateStore();
        const int concurrentCallCount = 50;

        var tasks = Enumerable.Range(0, concurrentCallCount)
            .Select(_ => store.TryConsumeAsync("demo-api-key-001", "nonce-concurrent"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(concurrentCallCount - 1, results.Count(r => !r));
    }
}
