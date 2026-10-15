using System.Net;
using System.Text.Json;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

[Binding]
public sealed class RedisReplayProtectionAndEpochSteps
{
    private RealStorageTestHarness _harness = null!;
    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;
    private readonly ReplayProtectionAndRetrySteps _replaySteps;

    public RedisReplayProtectionAndEpochSteps(ReplayProtectionAndRetrySteps replaySteps)
    {
        _replaySteps = replaySteps;
    }

    [Given("業務 API 執行環境已啟動並連線到真實 Redis")]
    public async Task GivenRedisReplayEnvironment()
    {
        _harness = await RealStorageTestHarness.GetAsync();
        await SpikeEnvironment.UseRedisReplayAsync(_harness.RedisConnection);
    }

    [Given("並且 第二個業務 API 執行個體已啟動並連線到相同 Redis")]
    [Given("第二個業務 API 執行個體已啟動並連線到相同 Redis")]
    public async Task GivenSecondaryWithSameRedis()
    {
        await Runtime.StartSecondaryOrdersApiAsync(SpikeEnvironment.FreeTcpPort());
    }

    [Then("最近一次請求被記錄為早於防重放紀元拒絕")]
    public void ThenLastRequestRecordedAsBeforeReplayEpoch()
    {
        var logs = Runtime.AuditLog.Snapshot();
        var last = logs.LastOrDefault();
        last.Should().NotBeNull("應該有產生稽核紀錄");
        last!.Reason.Should().Be("signature_before_replay_epoch");
    }

    [When("簽章呼叫端 {string} 以新時間與新 nonce 重新簽署並重試建立訂單")]
    public async Task WhenCallerRetriesWithNewTimeAndNonce(string clientId)
    {
        var epoch = await Runtime.ReplayStore.GetEpochAsync();
        if (epoch.HasValue)
        {
            while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= epoch.Value.ToUnixTimeSeconds())
            {
                await Task.Delay(100);
            }
        }

        // 取得上次發送的 Idempotency-Key
        await _replaySteps.WhenCallerRetriesWithNewNonce(clientId, Guid.NewGuid().ToString());
    }
}
