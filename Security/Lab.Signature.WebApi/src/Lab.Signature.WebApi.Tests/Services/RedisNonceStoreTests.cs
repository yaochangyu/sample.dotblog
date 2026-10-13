using Lab.Signature.WebApi.Services;
using NSubstitute;
using StackExchange.Redis;

namespace Lab.Signature.WebApi.Tests.Services;

public class RedisNonceStoreTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();

    public RedisNonceStoreTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_database);
    }

    [Fact]
    public async Task TryConsumeAsync_WhenKeyNotExists_ReturnsTrue()
    {
        _database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Is(When.NotExists))
            .Returns(true);

        var store = new RedisNonceStore(_redis);

        var result = await store.TryConsumeAsync("key-123", "NONCE-ABC");

        Assert.True(result);
    }

    [Fact]
    public async Task TryConsumeAsync_WhenKeyAlreadyExists_ReturnsFalse()
    {
        _database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Is(When.NotExists))
            .Returns(false);

        var store = new RedisNonceStore(_redis);

        var result = await store.TryConsumeAsync("key-123", "nonce-abc");

        Assert.False(result);
    }

    [Fact]
    public async Task TryConsumeAsync_NormalizesNonceToLowerCaseInKey()
    {
        RedisKey capturedKey = default;
        _database.StringSetAsync(
            Arg.Do<RedisKey>(k => capturedKey = k),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>())
            .Returns(true);

        var store = new RedisNonceStore(_redis);

        await store.TryConsumeAsync("demo-api-key", "UPPER-NONCE-123");

        Assert.Equal("api_nonce:demo-api-key:upper-nonce-123", (string)capturedKey!);
    }
}
