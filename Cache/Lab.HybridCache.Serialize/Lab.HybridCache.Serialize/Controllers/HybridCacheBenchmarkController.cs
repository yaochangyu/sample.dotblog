using System.Buffers;
using System.Diagnostics;
using System.Text;
using Lab.HybridCache.Serialize.Models;
using MemoryPack;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Lab.HybridCache.Serialize.Controllers;

[ApiController]
[Route("[controller]")]
public class HybridCacheBenchmarkController(
    IDistributedCache distributedCache,
    IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IServer _server = redis.GetServer(redis.GetEndPoints()[0]);
    private readonly IDatabase _db   = redis.GetDatabase();

    private const string MsgPackPrefix = "hc-msgpack";
    private const string MemPackPrefix = "hc-mempack";

    /// <summary>
    /// 執行 HybridCache L2 序列化比較（MessagePack vs MemoryPack），輸出 .md 報告
    /// </summary>
    [HttpPost("run/{count:int}")]
    public async Task<IActionResult> Run(int count, CancellationToken ct)
    {
        await CleanKeysAsync(MsgPackPrefix);
        await CleanKeysAsync(MemPackPrefix);

        var samples = Enumerable.Range(1, count)
            .Select(ProductModel.CreateSample)
            .ToList();

        // --- MessagePack L2 寫入（前後各取一次 used_memory）---
        var memBefore1 = await GetUsedMemoryAsync();
        var (msgSerMs, msgTotalMs) = await WriteWithMsgPackAsync(samples, ct);
        var memAfter1 = await GetUsedMemoryAsync();

        // --- MemoryPack L2 寫入 ---
        var memBefore2 = await GetUsedMemoryAsync();
        var (memSerMs, memTotalMs) = await WriteWithMemPackAsync(samples, ct);
        var memAfter2 = await GetUsedMemoryAsync();

        // --- 逐 key 量測（data field 大小 + MEMORY USAGE）---
        var msgStorage = await MeasureStorageAsync(MsgPackPrefix + ":*", memAfter1 - memBefore1);
        var memStorage = await MeasureStorageAsync(MemPackPrefix + ":*", memAfter2 - memBefore2);

        var report = BuildReport(count, msgSerMs, msgTotalMs, msgStorage, memSerMs, memTotalMs, memStorage);

        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "hybridcache-serializer-report.md");
        await System.IO.File.WriteAllTextAsync(reportPath, report, ct);

        return Ok(new
        {
            report_path = Path.GetFullPath(reportPath),
            count,
            messagepack = new
            {
                serialize_ms        = $"{msgSerMs:F2}",
                total_write_ms      = $"{msgTotalMs:F2}",
                data_bytes          = msgStorage.DataBytes,
                memory_usage_bytes  = msgStorage.MemoryUsageBytes,
                used_memory_delta   = msgStorage.UsedMemoryDelta
            },
            memorypack = new
            {
                serialize_ms        = $"{memSerMs:F2}",
                total_write_ms      = $"{memTotalMs:F2}",
                data_bytes          = memStorage.DataBytes,
                memory_usage_bytes  = memStorage.MemoryUsageBytes,
                used_memory_delta   = memStorage.UsedMemoryDelta
            },
            faster_serialize = msgSerMs  < memSerMs  ? "MessagePack" : "MemoryPack",
            smaller_in_redis = msgStorage.UsedMemoryDelta < memStorage.UsedMemoryDelta ? "MessagePack" : "MemoryPack"
        });
    }

    // ── 寫入 ──────────────────────────────────────────────────────────────

    private async Task<(double serializeMs, double totalMs)> WriteWithMsgPackAsync(
        List<ProductModel> samples, CancellationToken ct)
    {
        double serializeMs = 0;
        var sw = Stopwatch.StartNew();
        foreach (var product in samples)
        {
            var t = Stopwatch.StartNew();
            var buffer = new ArrayBufferWriter<byte>();
            MessagePackSerializer.Serialize(buffer, product);
            var bytes = buffer.WrittenSpan.ToArray();
            t.Stop();
            serializeMs += t.Elapsed.TotalMilliseconds;

            await distributedCache.SetAsync(
                $"{MsgPackPrefix}:{product.Id}", bytes, new DistributedCacheEntryOptions(), ct);
        }
        sw.Stop();
        return (serializeMs, sw.Elapsed.TotalMilliseconds);
    }

    private async Task<(double serializeMs, double totalMs)> WriteWithMemPackAsync(
        List<ProductModel> samples, CancellationToken ct)
    {
        double serializeMs = 0;
        var sw = Stopwatch.StartNew();
        foreach (var product in samples)
        {
            var t = Stopwatch.StartNew();
            var buffer = new ArrayBufferWriter<byte>();
            MemoryPackSerializer.Serialize(buffer, product);
            var bytes = buffer.WrittenSpan.ToArray();
            t.Stop();
            serializeMs += t.Elapsed.TotalMilliseconds;

            await distributedCache.SetAsync(
                $"{MemPackPrefix}:{product.Id}", bytes, new DistributedCacheEntryOptions(), ct);
        }
        sw.Stop();
        return (serializeMs, sw.Elapsed.TotalMilliseconds);
    }

    // ── 空間量測 ──────────────────────────────────────────────────────────

    private record StorageResult(int KeyCount, long DataBytes, long MemoryUsageBytes, long UsedMemoryDelta);

    private async Task<StorageResult> MeasureStorageAsync(string pattern, long usedMemoryDelta)
    {
        var count = 0;
        long dataBytes = 0;
        long memoryUsageBytes = 0;

        await foreach (var key in _server.KeysAsync(pattern: pattern))
        {
            // IDistributedCache (StackExchangeRedis) 以 Hash 儲存，data 欄位才是序列化後的 bytes
            var data = await _db.HashGetAsync(key, "data");
            if (data.HasValue) dataBytes += ((byte[])data!).Length;

            var mem = await _db.ExecuteAsync("MEMORY", "USAGE", key.ToString(), "SAMPLES", "0");
            if (!mem.IsNull) memoryUsageBytes += (long)mem;

            count++;
        }

        return new StorageResult(count, dataBytes, memoryUsageBytes, usedMemoryDelta);
    }

    // ── Redis INFO memory ──────────────────────────────────────────────────

    private async Task<long> GetUsedMemoryAsync()
    {
        var info = await _server.InfoAsync("memory");
        var entry = info.SelectMany(g => g).FirstOrDefault(e => e.Key == "used_memory");
        return long.TryParse(entry.Value, out var val) ? val : 0;
    }

    // ── 清除 ──────────────────────────────────────────────────────────────

    private async Task CleanKeysAsync(string prefix)
    {
        await foreach (var key in _server.KeysAsync(pattern: prefix + ":*"))
            await _db.KeyDeleteAsync(key);
    }

    // ── 報告 ──────────────────────────────────────────────────────────────

    private static string BuildReport(
        int count,
        double msgSerMs, double msgTotalMs, StorageResult msgS,
        double memSerMs, double memTotalMs, StorageResult memS)
    {
        var fasterSer     = msgSerMs  < memSerMs  ? "MessagePack ✓" : "MemoryPack ✓";
        var fasterTotal   = msgTotalMs < memTotalMs ? "MessagePack ✓" : "MemoryPack ✓";
        var smallerData   = msgS.DataBytes        < memS.DataBytes        ? "MessagePack ✓" : "MemoryPack ✓";
        var smallerUsage  = msgS.MemoryUsageBytes < memS.MemoryUsageBytes ? "MessagePack ✓" : "MemoryPack ✓";
        var smallerDelta  = msgS.UsedMemoryDelta  < memS.UsedMemoryDelta  ? "MessagePack ✓" : "MemoryPack ✓";

        var sb = new StringBuilder();
        sb.AppendLine("# HybridCache L2 序列化器比較報告");
        sb.AppendLine();
        sb.AppendLine("## 測試條件");
        sb.AppendLine();
        sb.AppendLine($"- 筆數：{count}");
        sb.AppendLine($"- L2 後端：Redis 7 (localhost:6379)");
        sb.AppendLine($"- 模型：`ProductModel`（int、string、decimal、DateTime、List、Dictionary）");
        sb.AppendLine($"- 報告產生時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## 實作方式");
        sb.AppendLine();
        sb.AppendLine("HybridCache 的 L2 寫入流程：");
        sb.AppendLine("```");
        sb.AppendLine("IHybridCacheSerializer<T>.Serialize(value, bufferWriter)");
        sb.AppendLine("  → IDistributedCache.SetAsync(key, bytes)");
        sb.AppendLine("    → Redis HSET key absexp <ticks> sldexp <ticks> data <bytes>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("| 格式 | 序列化器 | Redis Key 前綴 |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| HybridCache + MessagePack | `MessagePackSerializer.Serialize(bufferWriter, value)` | `hc-msgpack:{id}` |");
        sb.AppendLine("| HybridCache + MemoryPack  | `MemoryPackSerializer.Serialize(bufferWriter, value)`  | `hc-mempack:{id}` |");
        sb.AppendLine();
        sb.AppendLine("## 序列化速度");
        sb.AppendLine();
        sb.AppendLine("| 指標 | MessagePack | MemoryPack | 勝出 |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine($"| 純序列化時間（累計） | {msgSerMs:F2} ms | {memSerMs:F2} ms | {fasterSer} |");
        sb.AppendLine($"| L2 寫入總耗時（含 Redis I/O） | {msgTotalMs:F2} ms | {memTotalMs:F2} ms | {fasterTotal} |");
        sb.AppendLine($"| 每筆平均序列化 | {msgSerMs / count:F3} ms | {memSerMs / count:F3} ms | |");
        sb.AppendLine($"| 每筆平均 L2 寫入 | {msgTotalMs / count:F3} ms | {memTotalMs / count:F3} ms | |");
        sb.AppendLine();
        sb.AppendLine("## Redis 空間佔用");
        sb.AppendLine();
        sb.AppendLine("三種量測方式說明：");
        sb.AppendLine();
        sb.AppendLine("| 量測方式 | 命令 | 說明 |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| data field 大小 | `HGET key data` \\| len | Hash 中 `data` 欄位的純序列化 bytes，不含 Redis overhead |");
        sb.AppendLine("| MEMORY USAGE 累計 | `MEMORY USAGE key SAMPLES 0` | 含 key 名稱、Hash 結構、jemalloc 對齊的完整記憶體 |");
        sb.AppendLine("| used_memory 差值 | `INFO memory` 寫入前後差 | **Redis allocator 實際分配的增量**，最真實 |");
        sb.AppendLine();
        sb.AppendLine("| 指標 | MessagePack | MemoryPack | 勝出 |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine($"| Key 數量 | {msgS.KeyCount} | {memS.KeyCount} | |");
        sb.AppendLine($"| data field 大小 | {msgS.DataBytes:N0} bytes（{msgS.DataBytes / 1024.0:F2} KB） | {memS.DataBytes:N0} bytes（{memS.DataBytes / 1024.0:F2} KB） | {smallerData} |");
        sb.AppendLine($"| MEMORY USAGE 累計 | {msgS.MemoryUsageBytes:N0} bytes（{msgS.MemoryUsageBytes / 1024.0:F2} KB） | {memS.MemoryUsageBytes:N0} bytes（{memS.MemoryUsageBytes / 1024.0:F2} KB） | {smallerUsage} |");
        sb.AppendLine($"| **used_memory 差值** | **{msgS.UsedMemoryDelta:N0} bytes（{msgS.UsedMemoryDelta / 1024.0:F2} KB）** | **{memS.UsedMemoryDelta:N0} bytes（{memS.UsedMemoryDelta / 1024.0:F2} KB）** | **{smallerDelta}** |");
        sb.AppendLine($"| 每筆平均 data | {msgS.DataBytes / (double)count:F1} bytes | {memS.DataBytes / (double)count:F1} bytes | |");
        sb.AppendLine($"| 每筆平均 used_memory | {msgS.UsedMemoryDelta / (double)count:F1} bytes | {memS.UsedMemoryDelta / (double)count:F1} bytes | |");
        sb.AppendLine();
        sb.AppendLine("## 分析");
        sb.AppendLine();

        var valueDiff  = Math.Abs(msgS.DataBytes - memS.DataBytes);
        var valuePct   = valueDiff / (double)Math.Max(msgS.DataBytes, memS.DataBytes) * 100;
        var deltaFmt   = msgS.UsedMemoryDelta < memS.UsedMemoryDelta ? "MessagePack" : "MemoryPack";
        var deltaDiff  = Math.Abs(msgS.UsedMemoryDelta - memS.UsedMemoryDelta);
        var deltaPct   = deltaDiff / (double)Math.Max(msgS.UsedMemoryDelta, memS.UsedMemoryDelta) * 100;
        var fasterFmt  = msgSerMs < memSerMs ? "MessagePack" : "MemoryPack";
        var slowerFmt  = msgSerMs < memSerMs ? "MemoryPack" : "MessagePack";
        var serDiff    = Math.Abs(msgSerMs - memSerMs);

        sb.AppendLine("### 空間");
        sb.AppendLine($"- **{deltaFmt}** 寫入後 Redis `used_memory` 增加量較少，差距 {deltaDiff:N0} bytes（{deltaPct:F1}%）。");
        sb.AppendLine($"- data field 差異：{Math.Abs(msgS.DataBytes - memS.DataBytes):N0} bytes（{valuePct:F1}%），代表序列化格式本身的大小差。");
        sb.AppendLine($"- `used_memory` 差值 > `data field` 大小，因為 IDistributedCache 以 Hash 儲存（含 `absexp`、`sldexp` 欄位），加上 Redis jemalloc 對齊。");
        sb.AppendLine();
        sb.AppendLine("### 速度");
        sb.AppendLine($"- 純序列化：**{fasterFmt}** 比 {slowerFmt} 快 {serDiff:F2} ms（{count} 筆累計，每筆快 {serDiff / count:F3} ms）。");
        sb.AppendLine("- L2 寫入總耗時以 Redis I/O 為主，序列化差異相對被稀釋。");
        sb.AppendLine();
        sb.AppendLine("## 建議");
        sb.AppendLine();
        sb.AppendLine("| 場景 | 建議 |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| Redis 儲存成本敏感 | 選 `used_memory` 差值較小的格式 |");
        sb.AppendLine("| 高頻寫入、CPU 敏感 | 選純序列化較快的格式 |");
        sb.AppendLine("| 兩者差異在容忍範圍 | 優先考量生態系（MessagePack 跨語言較佳）|");

        return sb.ToString();
    }
}
