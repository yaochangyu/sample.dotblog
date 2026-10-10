using System.Net;
using System.Net.Sockets;
using AuthSpike.Hosting;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AuthSpike.Tests.Support;

/// <summary>整個測試執行共用一組真實 TLS 端點（授權伺服器與建立訂單 API）；標示 @short-lifetime 的 Scenario 會暫時換用短效 Token 環境。</summary>
public static class SpikeEnvironment
{
    private static SpikeRuntime? _defaultRuntime;

    public static SpikeRuntime Runtime { get; private set; } = null!;

    public static async Task StartAsync()
    {
        _defaultRuntime = await SpikeRuntime.StartAsync(FreeTcpPort(), FreeTcpPort());
        Runtime = _defaultRuntime;
    }

    public static async Task StopAsync()
    {
        if (Runtime is not null)
        {
            await Runtime.DisposeAsync();
        }
    }

    public static async Task UseShortLivedTokensAsync()
    {
        Runtime = await SpikeRuntime.StartAsync(FreeTcpPort(), FreeTcpPort(), TimeSpan.FromSeconds(2));
    }

    /// <summary>標示 @isolated 的 Scenario 使用獨立執行環境（防重放儲存、執行個體與故障模擬不影響其他 Scenario）。</summary>
    public static async Task UseIsolatedRuntimeAsync(TimeSpan? verificationCacheLifetime = null)
    {
        Runtime = await SpikeRuntime.StartAsync(FreeTcpPort(), FreeTcpPort(), verificationCacheLifetime: verificationCacheLifetime);
    }

    public static async Task RestoreDefaultTokensAsync()
    {
        await Runtime.DisposeAsync();
        Runtime = _defaultRuntime!;
    }

    public static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
