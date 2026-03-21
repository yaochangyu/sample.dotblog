using Lab.HybridCache.Serialize.Models;
using Microsoft.AspNetCore.Mvc;
using MsHybridCache = Microsoft.Extensions.Caching.Hybrid.HybridCache;

namespace Lab.HybridCache.Serialize.Controllers;

[ApiController]
[Route("[controller]")]
public class HybridCacheController(MsHybridCache cache) : ControllerBase
{
    /// <summary>
    /// 取得商品（L1 → L2 → 產生資料）。
    /// 序列化器由 Program.cs 的 AddSerializer 決定，切換序列化格式只需改那一行。
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var product = await cache.GetOrCreateAsync(
            key: $"hybrid:product:{id}",
            factory: _ => ValueTask.FromResult(ProductModel.CreateSample(id)),
            cancellationToken: ct);

        sw.Stop();

        return Ok(new
        {
            elapsed_ms = $"{sw.Elapsed.TotalMilliseconds:F3}",
            product
        });
    }

    /// <summary>
    /// 移除指定 id 的快取（L1 + L2 同時失效）
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Evict(int id, CancellationToken ct)
    {
        await cache.RemoveAsync($"hybrid:product:{id}", ct);
        return Ok(new { message = $"hybrid:product:{id} 已從快取移除" });
    }
}
