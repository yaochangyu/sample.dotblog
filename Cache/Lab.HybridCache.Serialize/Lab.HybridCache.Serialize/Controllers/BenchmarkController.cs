using Lab.HybirdCache.Compress.Models;
using MessagePack;
using MemoryPack;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Lab.HybirdCache.Compress.Controllers;

[ApiController]
[Route("[controller]")]
public class BenchmarkController(IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IDatabase _db = redis.GetDatabase();

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
