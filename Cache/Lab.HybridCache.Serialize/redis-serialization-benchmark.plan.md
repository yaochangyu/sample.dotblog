# Redis 序列化格式比較報告

## 環境

- .NET 10
- StackExchange.Redis 2.12.4
- MessagePack 3.1.4
- MemoryPack 1.21.4
- K4os.Compression.LZ4 1.3.8
- Redis 7 (Alpine, Docker)

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

每筆資料 Description、Tags、Metadata 內容固定，只有 Id、Name、Price、Stock 隨 id 變化。

---

## 實作說明

### 四種格式如何產生

| 格式 | 套件 | 做法 |
|---|---|---|
| MessagePack | `MessagePack` | `MessagePackSerializer.Serialize(product)` |
| MessagePack+LZ4 | `MessagePack`（內建） | `MessagePackSerializer.Serialize(product, options)` 其中 `options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block)` |
| MemoryPack | `MemoryPack` | `MemoryPackSerializer.Serialize(product)` |
| MemoryPack+LZ4 | `MemoryPack` + `K4os.Compression.LZ4` | 先 `MemoryPackSerializer.Serialize(product)` 取得 `byte[]`，再用 `LZ4Pickler.Pickle(bytes)` 壓縮 |

> MessagePack 的 LZ4 是在序列化過程中整合的；MemoryPack 的 LZ4 是事後對序列化結果再壓縮，兩個步驟分離。

### 空間量測方式

兩種指標並列：

| 指標 | Redis 命令 | 說明 |
|---|---|---|
| `value_bytes` | `STRLEN key` | 純 value 大小，不含任何 Redis overhead |
| `redis_memory_bytes` | `MEMORY USAGE key SAMPLES 0` | Redis 實際佔用記憶體，含 key 名稱、encoding、jemalloc 對齊等 overhead；`SAMPLES 0` 表示精確計算不抽樣 |

使用 `IServer.KeysAsync(pattern)` 以 SCAN 方式掃描所有符合前綴的 key，逐一累加。

### 速度量測方式

使用 `System.Diagnostics.Stopwatch`，在純記憶體中對 N 筆樣本循序序列化（不含 Redis I/O），避免網路延遲干擾結果。

```
samples = CreateSample(1..N)
sw.Start()
foreach p in samples → Serialize(p)
sw.Stop()
```

---

## 測試結果（1000 筆）

### 空間佔用

| 格式 | 純資料 (value_bytes) | Redis 記憶體 | 與原始相比 |
|---|---|---|---|
| MessagePack | **225.03 KB** ✓ | **304.69 KB** ✓ | — |
| MessagePack+LZ4 | 226.77 KB | 311.73 KB | **+1.74 KB（反而變大）** |
| MemoryPack | 359.27 KB | 429.69 KB | — |
| MemoryPack+LZ4 | 307.17 KB | 410.10 KB | 省 52.10 KB（-14.5%）|

### 單筆壓縮前後（id=1）

| 格式 | 原始 bytes | LZ4 後 bytes | 壓縮率 | 省下 |
|---|---|---|---|---|
| MessagePack | 225 | 227 | 100.9% | **-2（膨脹）** |
| MemoryPack | 366 | 312 | 85.2% | +54 |

### 序列化速度（1000 筆，5 次平均）

| 格式 | 平均 ms | LZ4 額外成本 |
|---|---|---|
| MessagePack | **~0.34 ms** ✓ | — |
| MemoryPack | ~0.56 ms | — |
| MessagePack+LZ4 | ~2.30 ms | +1.96 ms（約 6.8x） |
| MemoryPack+LZ4 | ~6.21 ms | +5.65 ms（約 11x） |

---

## 分析

### 為什麼 MessagePack+LZ4 反而變大？

MessagePack 本身的 binary 格式已高度緊湊，每筆資料僅 225 bytes。LZ4 Block 模式壓縮時會加入固定 header（magic number + 壓縮長度欄位等，約 10+ bytes），但資料量太小，LZ4 找不到足夠的重複模式可以壓縮，最終 header overhead 大於節省量。

### 為什麼 MemoryPack+LZ4 有效？

MemoryPack 輸出格式為接近記憶體排列的 binary，包含較多對齊填充（padding），重複 pattern 較多（如 string 的 length prefix、固定欄位值），LZ4 可有效壓縮，節省約 14.5%。

### LZ4 速度成本

LZ4 雖然是目前最快的壓縮演算法之一，但對於千筆小資料的場景，壓縮運算本身的 CPU 時間遠大於序列化本身：

- MessagePack+LZ4：額外約 +6.8x 時間成本
- MemoryPack+LZ4：額外約 +11x 時間成本

---

## 選用建議

| 場景 | 建議格式 |
|---|---|
| 最小空間、最均衡 | **MessagePack** |
| 最快序列化速度 | **MessagePack** |
| MemoryPack 資料需壓縮 | **MemoryPack+LZ4**（可省 ~14%）|
| 不建議 | MessagePack+LZ4（資料小時會膨脹） |
