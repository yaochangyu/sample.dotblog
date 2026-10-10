using AuthSpike.Tests.Support;
using Reqnroll;

namespace AuthSpike.Tests.Support;

[Binding]
public static class SpikeHooks
{
    [BeforeTestRun]
    public static Task StartEnvironment() => SpikeEnvironment.StartAsync();

    [AfterTestRun]
    public static Task StopEnvironment() => SpikeEnvironment.StopAsync();

    [BeforeScenario("@short-lifetime")]
    public static Task UseShortLivedTokens() => SpikeEnvironment.UseShortLivedTokensAsync();

    [AfterScenario("@short-lifetime")]
    public static Task RestoreDefaultTokens() => SpikeEnvironment.RestoreDefaultTokensAsync();

    /// <summary>標示 @short-cache 的 Scenario 使用獨立環境與 4 秒的 Token 查證快取，便於驗證快取到期後的行為。</summary>
    [BeforeScenario("@short-cache")]
    public static Task UseShortCache() => SpikeEnvironment.UseIsolatedRuntimeAsync(TimeSpan.FromSeconds(4));

    [AfterScenario("@short-cache")]
    public static Task RestoreAfterShortCache() => SpikeEnvironment.RestoreDefaultTokensAsync();

    [BeforeScenario("@isolated")]
    public static Task UseIsolatedRuntime() => SpikeEnvironment.UseIsolatedRuntimeAsync();

    [AfterScenario("@isolated")]
    public static Task RestoreDefaultRuntime() => SpikeEnvironment.RestoreDefaultTokensAsync();
}
