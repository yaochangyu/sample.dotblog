# MessagePack vs MemoryPack 寫入 Redis：速度、空間、HybridCache 全面比較

## 前言

在 .NET 中把物件快取到 Redis，序列化格式的選擇直接影響：

- **速度**：CPU 序列化成本
- **空間**：Redis 記憶體用量
- **生態**：跨語言支援

本文實測三種組合（共六種格式），給出數字和結論。

---

## 環境

| 項目 | 版本 |
|---|---|
| .NET | 10 |
| MessagePack | 3.1.4 |
| MemoryPack | 1.21.4 |
| K4os.Compression.LZ4 | 1.3.8 |
| Microsoft.Extensions.Caching.Hybrid | 10.4.0 |
| Redis | 7（Docker Alpine） |

---

## 測試資料模型

```csharp
[MessagePackObject]
[MemoryPackable]
public partial class ProductModel
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string Name { get; set; }
    [Key(2)] public string Description { get; set; }   // 固定長字串
    [Key(3)] public decimal Price { get; set; }
    [Key(4)] public int Stock { get; set; }
    [Key(5)] public DateTime CreatedAt { get; set; }
    [Key(6)] public List<string> Tags { get; set; }    // 5 個固定 tag
    [Key(7)] public Dictionary<string, string> Metadata { get; set; }  // 5 個鍵值對
}
```

同時掛上 `[MessagePackObject]` 和 `[MemoryPackable]`，兩套序列化共用同一個模型。

---

## 組合一、二：直接寫入 Redis（含 LZ4）

### 四種格式怎麼產生的

| 格式 | 做法 |
|---|---|
| MessagePack | `MessagePackSerializer.Serialize(product)` |
| MessagePack+LZ4 | 同上，options 加 `WithCompression(Lz4Block)` |
| MemoryPack | `MemoryPackSerializer.Serialize(product)` |
| MemoryPack+LZ4 | 先 `MemoryPackSerializer.Serialize`，再 `LZ4Pickler.Pickle` |

> MessagePack 的 LZ4 整合在序列化過程；MemoryPack 的 LZ4 是事後壓縮，兩個步驟分開。

```csharp
// POST /benchmark/write/{id}
var msgPackBytes    = MessagePackSerializer.Serialize(product);
var msgPackLz4Bytes = MessagePackSerializer.Serialize(product, MsgPackLz4Options);
var memPackBytes    = MemoryPackSerializer.Serialize(product);
var memPackLz4Bytes = LZ4Pickler.Pickle(memPackBytes);

await Task.WhenAll(
    _db.StringSetAsync($"msgpack:{id}",      msgPackBytes),
    _db.StringSetAsync($"msgpack-lz4:{id}",  msgPackLz4Bytes),
    _db.StringSetAsync($"mempack:{id}",      memPackBytes),
    _db.StringSetAsync($"mempack-lz4:{id}",  memPackLz4Bytes)
);
```

### 空間佔用（1000 筆）

| 格式 | 純 value（value_bytes） | Redis 記憶體（MEMORY USAGE） | 與原始格式相比 |
|---|---|---|---|
| MessagePack | **225.03 KB** ✓ | **304.69 KB** ✓ | — |
| MessagePack+LZ4 | 226.77 KB | 311.73 KB | **+1.74 KB（膨脹）** |
| MemoryPack | 359.27 KB | 429.69 KB | — |
| MemoryPack+LZ4 | 307.17 KB | 410.10 KB | 省 52.10 KB（-14.5%） |

### 序列化速度（1000 筆，5 次取 Run3–5 平均，排除 JIT 暖機）

| 格式 | 平均 ms | LZ4 額外成本 |
|---|---|---|
| MessagePack | ~0.27 ms | — |
| **MemoryPack** | **~0.23 ms** ✓ | — |
| MessagePack+LZ4 | ~0.63 ms | +0.36 ms（約 2.3x） |
| MemoryPack+LZ4 | ~1.19 ms | +0.96 ms（約 5.2x） |

### 為什麼 MessagePack+LZ4 反而變大？

MessagePack 本身已很緊湊（每筆約 225 bytes），LZ4 Block 模式有固定 header overhead（約 10+ bytes），資料太小找不到足夠的重複 pattern，結果 header 比節省的還大。

MemoryPack 輸出接近記憶體排列，含較多對齊 padding，重複 pattern 多，LZ4 可壓約 14.5%。

---

## 組合三：HybridCache L2 序列化

HybridCache 的 L2 寫入流程：

```
IHybridCacheSerializer<T>.Serialize(value, bufferWriter)
  → IDistributedCache.SetAsync(key, bytes)
    → Redis HSET key absexp <ticks> sldexp <ticks> data <bytes>
```

`IDistributedCache` 以 **Redis Hash** 儲存，每個 key 有三個欄位：`absexp`、`sldexp`、`data`。

### 序列化器實作

```csharp
// MessagePack 版本
public sealed class MessagePackHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public void Serialize(T value, IBufferWriter<byte> target)
        => MessagePackSerializer.Serialize(target, value, _options);

    public T Deserialize(ReadOnlySequence<byte> source)
        => MessagePackSerializer.Deserialize<T>(source, _options);
}

// MemoryPack 版本
public sealed class MemoryPackHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public void Serialize(T value, IBufferWriter<byte> target)
        => MemoryPackSerializer.Serialize(target, value);

    public T Deserialize(ReadOnlySequence<byte> source)
        => MemoryPackSerializer.Deserialize<T>(source)
           ?? throw new InvalidDataException($"MemoryPack 無法反序列化 {typeof(T).Name}");
}
```

### 在 DI 註冊

```csharp
// Program.cs
builder.Services.AddHybridCache()
    .AddSerializer<ProductModel, MessagePackHybridCacheSerializer<ProductModel>>();
    // 切換 MemoryPack：
    // .AddSerializer<ProductModel, MemoryPackHybridCacheSerializer<ProductModel>>();
```

### 為什麼 HybridCache 的空間量測要用三種方式？

直接 `STRLEN key` 在 Hash 結構下不適用，需要：

| 量測方式 | 命令 | 說明 |
|---|---|---|
| data field 大小 | `HGET key data` ｜ len | 純序列化 bytes，最小值 |
| MEMORY USAGE | `MEMORY USAGE key SAMPLES 0` | 含 key 名稱、Hash 結構、jemalloc 對齊 |
| **used_memory 差值** | `INFO memory` 寫入前後相減 | **Redis allocator 實際分配增量，最真實** |

### 結果（1000 筆）

| 指標 | MessagePack | MemoryPack | 勝出 |
|---|---|---|---|
| data field 大小 | **225.03 KB** | 359.27 KB | MessagePack（少 37.4%）|
| MEMORY USAGE 累計 | **500.01 KB** | 625.01 KB | MessagePack |
| **used_memory 差值** | 607.94 KB | **595.01 KB** | **MemoryPack（少 2.1%）** |
| 純序列化速度（1000 筆） | 4.20 ms | **3.68 ms** | MemoryPack（快 1.1x）|
| L2 寫入總耗時（含 Redis I/O） | 211.32 ms | **190.34 ms** | MemoryPack（快 1.1x）|

### 關鍵觀察

- **data field 差 37%，但 `used_memory` 差值只差 2.1%**：Hash overhead（`absexp`、`sldexp`）加上 jemalloc 對齊是固定成本，稀釋了序列化格式本身的差異。
- 「序列化格式省多少空間」≠「Redis 真正省多少空間」，應以 `used_memory` 差值為準。
- **`used_memory` 差值 MemoryPack 反而略佔優**：jemalloc 對 MessagePack 較小 data block 的對齊方式使整體 overhead 比 MemoryPack 更高。
- L2 寫入以 Redis I/O 為主，兩者速度差距極小（4.20 ms vs 3.68 ms）。

---

## 整合比較與建議

| 場景 | 建議 |
|---|---|
| Redis 純 value 空間最小、跨語言相容 | **MessagePack** |
| Redis 實際記憶體（used_memory）最小 | **MemoryPack**（HybridCache 場景，少 2.1%）|
| 高頻寫入、CPU 敏感 | **MemoryPack**（序列化快 1.2x）|
| MemoryPack 需縮空間（直接 Redis）| **MemoryPack+LZ4**（省 ~14.5%）|
| 避免使用 | MessagePack+LZ4（資料小時反而膨脹）|

---

## 專案結構

```
Lab.HybridCache.Serialize/
├── Models/
│   └── ProductModel.cs                     # 同時支援 MessagePack + MemoryPack
├── Serializers/
│   ├── MessagePackHybridCacheSerializer.cs # IHybridCacheSerializer 實作
│   └── MemoryPackHybridCacheSerializer.cs
└── Controllers/
    ├── BenchmarkController.cs              # 組合一、二：直接寫 Redis（含 LZ4）
    ├── HybridCacheBenchmarkController.cs   # 組合三：HybridCache L2 比較，輸出 .md 報告
    └── HybridCacheController.cs            # GetOrCreate / Evict
```

---

## 完整程式碼

[GitHub - sample.dotblog/Cache/Lab.HybridCache.Serialize](https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HybridCache.Serialize)
