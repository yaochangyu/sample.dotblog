using K4os.Compression.LZ4;
using Lab.HybridCache.Serialize.Models;
using MemoryPack;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Lab.HybridCache.Serialize.Controllers;

[ApiController]
[Route("[controller]")]
public class BenchmarkController(IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly IServer _server = redis.GetServer(redis.GetEndPoints()[0]);

    private static readonly MessagePackSerializerOptions MsgPackLz4Options =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

    /// <summary>
    /// 將同一筆資料分別以 MessagePack 與 MemoryPack 序列化後寫入 Redis
    /// </summary>
    [HttpPost("write/{id:int}")]
    public async Task<IActionResult> Write(int id)
    {
        var product = ProductModel.CreateSample(id);

        var msgPackBytes = MessagePackSerializer.Serialize(product);
        var memPackBytes = MemoryPackSerializer.Serialize(product);

        await Task.WhenAll(
            _db.StringSetAsync($"msgpack:{id}", msgPackBytes),
            _db.StringSetAsync($"mempack:{id}", memPackBytes)
        );

        return Ok(new
        {
            id,
            messagepack_bytes = msgPackBytes.Length,
            memorypack_bytes = memPackBytes.Length
        });
    }

    /// <summary>
    /// 將同一筆資料以 4 種格式（含 LZ4）寫入 Redis
    /// </summary>
    [HttpPost("write-compress/{id:int}")]
    public async Task<IActionResult> WriteCompress(int id)
    {
        var product = ProductModel.CreateSample(id);

        var msgPackBytes = MessagePackSerializer.Serialize(product);
        var msgPackLz4Bytes = MessagePackSerializer.Serialize(product, MsgPackLz4Options);
        var memPackBytes = MemoryPackSerializer.Serialize(product);
        var memPackLz4Bytes = LZ4Pickler.Pickle(memPackBytes);

        await Task.WhenAll(
            _db.StringSetAsync($"msgpack:{id}", msgPackBytes),
            _db.StringSetAsync($"msgpack-lz4:{id}", msgPackLz4Bytes),
            _db.StringSetAsync($"mempack:{id}", memPackBytes),
            _db.StringSetAsync($"mempack-lz4:{id}", memPackLz4Bytes)
        );

        return Ok(new
        {
            id,
            messagepack = new { bytes = msgPackBytes.Length },
            messagepack_lz4 = new
            {
                bytes = msgPackLz4Bytes.Length,
                ratio = $"{(double)msgPackLz4Bytes.Length / msgPackBytes.Length * 100:F1}%"
            },
            memorypack = new { bytes = memPackBytes.Length },
            memorypack_lz4 = new
            {
                bytes = memPackLz4Bytes.Length,
                ratio = $"{(double)memPackLz4Bytes.Length / memPackBytes.Length * 100:F1}%"
            }
        });
    }

    /// <summary>
    /// 從 Redis 讀取兩種序列化的 byte 大小並比較
    /// </summary>
    [HttpGet("stats/{id:int}")]
    public async Task<IActionResult> Stats(int id)
    {
        var msgPackKey = $"msgpack:{id}";
        var memPackKey = $"mempack:{id}";

        var msgPackValue = await _db.StringGetAsync(msgPackKey);
        var memPackValue = await _db.StringGetAsync(memPackKey);

        if (!msgPackValue.HasValue || !memPackValue.HasValue)
            return NotFound(new { message = $"請先呼叫 POST /benchmark/write/{id}" });

        var msgPackSize = ((byte[])msgPackValue!).Length;
        var memPackSize = ((byte[])memPackValue!).Length;

        var smaller = msgPackSize < memPackSize ? "MessagePack" : "MemoryPack";
        var diff = Math.Abs(msgPackSize - memPackSize);
        var ratio = (double)Math.Min(msgPackSize, memPackSize) / Math.Max(msgPackSize, memPackSize) * 100;

        return Ok(new
        {
            id,
            messagepack = new { key = msgPackKey, bytes = msgPackSize },
            memorypack = new { key = memPackKey, bytes = memPackSize },
            winner = smaller,
            diff_bytes = diff,
            winner_ratio = $"{ratio:F1}% of loser size"
        });
    }

    /// <summary>
    /// 從 Redis 讀取 4 種格式，比較壓縮前後的 byte 大小與壓縮率
    /// </summary>
    [HttpGet("stats-compress/{id:int}")]
    public async Task<IActionResult> StatsCompress(int id)
    {
        var keys = new[]
        {
            $"msgpack:{id}", $"msgpack-lz4:{id}",
            $"mempack:{id}", $"mempack-lz4:{id}"
        };

        var sizes = await Task.WhenAll(keys.Select(k => _db.StringLengthAsync(k)));

        if (sizes.Any(s => s == 0))
            return NotFound(new { message = $"請先呼叫 POST /benchmark/write-compress/{id}" });

        var msgPack = sizes[0];
        var msgPackLz4 = sizes[1];
        var memPack = sizes[2];
        var memPackLz4 = sizes[3];

        return Ok(new
        {
            id,
            messagepack = new
            {
                original_bytes = msgPack,
                lz4_bytes = msgPackLz4,
                lz4_ratio = $"{(double)msgPackLz4 / msgPack * 100:F1}%",
                saved_bytes = msgPack - msgPackLz4
            },
            memorypack = new
            {
                original_bytes = memPack,
                lz4_bytes = memPackLz4,
                lz4_ratio = $"{(double)memPackLz4 / memPack * 100:F1}%",
                saved_bytes = memPack - memPackLz4
            },
            overall_winner = new[]
            {
                ("MessagePack", msgPack),
                ("MessagePack+LZ4", msgPackLz4),
                ("MemoryPack", memPack),
                ("MemoryPack+LZ4", memPackLz4)
            }.MinBy(x => x.Item2).Item1
        });
    }

    /// <summary>
    /// 統計 msgpack:* 與 mempack:* 所有 key 在 Redis 中佔用的實際記憶體（含 LZ4 系列）
    /// </summary>
    [HttpGet("storage")]
    public async Task<IActionResult> Storage()
    {
        var (msgPackCount, msgPackMemory, msgPackValue) =
            await SumStorageAsync(_server.KeysAsync(pattern: "msgpack:*"));
        var (msgLz4Count, msgLz4Memory, msgLz4Value) =
            await SumStorageAsync(_server.KeysAsync(pattern: "msgpack-lz4:*"));
        var (memPackCount, memPackMemory, memPackValue) =
            await SumStorageAsync(_server.KeysAsync(pattern: "mempack:*"));
        var (memLz4Count, memLz4Memory, memLz4Value) =
            await SumStorageAsync(_server.KeysAsync(pattern: "mempack-lz4:*"));

        return Ok(new
        {
            messagepack = new
            {
                key_count = msgPackCount, value_bytes = msgPackValue, value_kb = $"{msgPackValue / 1024.0:F2} KB",
                redis_memory_bytes = msgPackMemory, redis_memory_kb = $"{msgPackMemory / 1024.0:F2} KB"
            },
            messagepack_lz4 = new
            {
                key_count = msgLz4Count, value_bytes = msgLz4Value, value_kb = $"{msgLz4Value / 1024.0:F2} KB",
                redis_memory_bytes = msgLz4Memory, redis_memory_kb = $"{msgLz4Memory / 1024.0:F2} KB",
                saved_vs_original_kb = $"{(msgPackValue - msgLz4Value) / 1024.0:F2} KB"
            },
            memorypack = new
            {
                key_count = memPackCount, value_bytes = memPackValue, value_kb = $"{memPackValue / 1024.0:F2} KB",
                redis_memory_bytes = memPackMemory, redis_memory_kb = $"{memPackMemory / 1024.0:F2} KB"
            },
            memorypack_lz4 = new
            {
                key_count = memLz4Count, value_bytes = memLz4Value, value_kb = $"{memLz4Value / 1024.0:F2} KB",
                redis_memory_bytes = memLz4Memory, redis_memory_kb = $"{memLz4Memory / 1024.0:F2} KB",
                saved_vs_original_kb = $"{(memPackValue - memLz4Value) / 1024.0:F2} KB"
            },
            overall_winner_by_value = new[]
            {
                ("MessagePack", msgPackValue),
                ("MessagePack+LZ4", msgLz4Value),
                ("MemoryPack", memPackValue),
                ("MemoryPack+LZ4", memLz4Value)
            }.MinBy(x => x.Item2).Item1
        });
    }

    private async Task<(int count, long memoryBytes, long valueBytes)> SumStorageAsync(IAsyncEnumerable<RedisKey> keys)
    {
        var count = 0;
        long memoryBytes = 0;
        long valueBytes = 0;
        await foreach (var key in keys)
        {
            var memResult = await _db.ExecuteAsync("MEMORY", "USAGE", key.ToString(), "SAMPLES", "0");
            if (!memResult.IsNull)
                memoryBytes += (long)memResult;

            valueBytes += await _db.StringLengthAsync(key);
            count++;
        }

        return (count, memoryBytes, valueBytes);
    }

    /// <summary>
    /// 純序列化速度比較（不寫 Redis），單位 ms
    /// </summary>
    [HttpGet("speed/{count:int}")]
    public IActionResult Speed(int count)
    {
        var samples = Enumerable.Range(1, count).Select(ProductModel.CreateSample).ToList();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var p in samples) MessagePackSerializer.Serialize(p);
        sw.Stop();
        var msgPackMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        foreach (var p in samples) MemoryPackSerializer.Serialize(p);
        sw.Stop();
        var memPackMs = sw.Elapsed.TotalMilliseconds;

        var faster = msgPackMs < memPackMs ? "MessagePack" : "MemoryPack";
        return Ok(new
        {
            count,
            messagepack_ms = $"{msgPackMs:F2}",
            memorypack_ms = $"{memPackMs:F2}",
            faster,
            speedup = $"{Math.Max(msgPackMs, memPackMs) / Math.Min(msgPackMs, memPackMs):F2}x"
        });
    }

    /// <summary>
    /// 4 種格式（含 LZ4）的序列化速度比較，單位 ms
    /// </summary>
    [HttpGet("speed-compress/{count:int}")]
    public IActionResult SpeedCompress(int count)
    {
        var samples = Enumerable.Range(1, count).Select(ProductModel.CreateSample).ToList();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        sw.Restart();
        foreach (var p in samples) MessagePackSerializer.Serialize(p);
        sw.Stop();
        var msgPackMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        foreach (var p in samples) MessagePackSerializer.Serialize(p, MsgPackLz4Options);
        sw.Stop();
        var msgPackLz4Ms = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        foreach (var p in samples) MemoryPackSerializer.Serialize(p);
        sw.Stop();
        var memPackMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        foreach (var p in samples) LZ4Pickler.Pickle(MemoryPackSerializer.Serialize(p));
        sw.Stop();
        var memPackLz4Ms = sw.Elapsed.TotalMilliseconds;

        var results = new[]
        {
            ("MessagePack", msgPackMs),
            ("MessagePack+LZ4", msgPackLz4Ms),
            ("MemoryPack", memPackMs),
            ("MemoryPack+LZ4", memPackLz4Ms)
        };
        var fastest = results.MinBy(x => x.Item2).Item1;

        return Ok(new
        {
            count,
            messagepack_ms = $"{msgPackMs:F2}",
            messagepack_lz4_ms = $"{msgPackLz4Ms:F2}",
            memorypack_ms = $"{memPackMs:F2}",
            memorypack_lz4_ms = $"{memPackLz4Ms:F2}",
            fastest,
            lz4_overhead = new
            {
                messagepack = $"+{msgPackLz4Ms - msgPackMs:F2} ms",
                memorypack = $"+{memPackLz4Ms - memPackMs:F2} ms"
            }
        });
    }

    /// <summary>
    /// 批次寫入多筆資料並回傳彙總統計
    /// </summary>
    [HttpPost("batch/{count:int}")]
    public async Task<IActionResult> Batch(int count)
    {
        long totalMsgPack = 0;
        long totalMemPack = 0;

        var tasks = Enumerable.Range(1, count).Select(async i =>
        {
            var product = ProductModel.CreateSample(i);
            var msgPackBytes = MessagePackSerializer.Serialize(product);
            var memPackBytes = MemoryPackSerializer.Serialize(product);

            Interlocked.Add(ref totalMsgPack, msgPackBytes.Length);
            Interlocked.Add(ref totalMemPack, memPackBytes.Length);

            await Task.WhenAll(
                _db.StringSetAsync($"msgpack:{i}", msgPackBytes),
                _db.StringSetAsync($"mempack:{i}", memPackBytes)
            );
        });

        await Task.WhenAll(tasks);

        var smaller = totalMsgPack < totalMemPack ? "MessagePack" : "MemoryPack";
        var saved = Math.Abs(totalMsgPack - totalMemPack);

        return Ok(new
        {
            count,
            messagepack_total_bytes = totalMsgPack,
            memorypack_total_bytes = totalMemPack,
            winner = smaller,
            saved_bytes = saved,
            saved_kb = $"{saved / 1024.0:F2} KB"
        });
    }
}