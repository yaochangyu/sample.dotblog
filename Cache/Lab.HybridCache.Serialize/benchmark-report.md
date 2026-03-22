# MessagePack vs MemoryPack 序列化比較報告

## 環境

- .NET 10
- StackExchange.Redis 2.12.4
- MessagePack 3.1.4
- MemoryPack 1.21.4
- K4os.Compression.LZ4 1.3.8
- Redis 7（Alpine, Docker, localhost:6379）

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

## 測試條件

- 筆數：1000
- 速度量測：共 5 次，取後 3 次（暖機後）平均
- 報告產生時間：2026-03-22 10:53:06

---

## 實作說明

### 四種格式如何產生

| 格式 | 套件 | 做法 |
|---|---|---|
| MessagePack | `MessagePack` | `MessagePackSerializer.Serialize(product)` |
| MessagePack+LZ4 | `MessagePack`（內建） | `MessagePackSerializer.Serialize(product, options)`，其中 `options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block)` |
| MemoryPack | `MemoryPack` | `MemoryPackSerializer.Serialize(product)` |
| MemoryPack+LZ4 | `MemoryPack` + `K4os.Compression.LZ4` | 先 `MemoryPackSerializer.Serialize(product)` 取得 `byte[]`，再用 `LZ4Pickler.Pickle(bytes)` 壓縮 |

> MessagePack 的 LZ4 是在序列化過程中整合的；MemoryPack 的 LZ4 是事後對序列化結果再壓縮，兩個步驟分離。

### 空間量測方式

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

## 組合一、二：直接寫入 Redis（含 LZ4）

### 空間佔用

| 格式 | value bytes | value KB | Redis MEMORY USAGE | 與原始比 |
|---|---:|---:|---:|---|
| MessagePack | 230,429 | **225.03 KB** ✓ | 304.69 KB | — |
| MessagePack+LZ4 | 232,209 | 226.77 KB | 311.73 KB | **膨脹 +1.74 KB（+0.8%）** |
| MemoryPack | 367,893 | 359.27 KB | 429.69 KB | — |
| MemoryPack+LZ4 | 314,545 | 307.17 KB | 410.10 KB | 省 52.10 KB（-14.5%） |

### 單筆壓縮前後（id=1）

| 格式 | 原始 bytes | LZ4 後 bytes | 壓縮率 | 省下 |
|---|---:|---:|---:|---|
| MessagePack | 225 | 227 | 100.9% | **-2（膨脹）** |
| MemoryPack | 366 | 312 | 85.2% | +54 |

### 序列化速度（暖機後 3 次平均）

| 格式 | 平均 ms | LZ4 額外倍數 |
|---|---:|---:|
| MessagePack | 1.41 ms | — |
| MessagePack+LZ4 | 3.19 ms | 2.3x |
| MemoryPack | 1.30 ms | — |
| MemoryPack+LZ4 | 8.37 ms | 6.5x |
| **序列化最快** | **MemoryPack ✓** | |

### 分析

**為什麼 MessagePack+LZ4 反而變大？**

MessagePack 本身的 binary 格式已高度緊湊，每筆資料僅 225 bytes。LZ4 Block 模式壓縮時會加入固定 header（magic number + 壓縮長度欄位等，約 10+ bytes），但資料量太小，LZ4 找不到足夠的重複模式可以壓縮，最終 header overhead 大於節省量。

**為什麼 MemoryPack+LZ4 有效？**

MemoryPack 輸出格式為接近記憶體排列的 binary，包含較多對齊填充（padding），重複 pattern 較多（如 string 的 length prefix、固定欄位值），LZ4 可有效壓縮，節省約 14.5%。

**LZ4 速度成本**

LZ4 雖然是目前最快的壓縮演算法之一，但對於千筆小資料的場景，壓縮運算仍有額外負擔：

- MessagePack+LZ4：額外約 +2.3x 時間成本
- MemoryPack+LZ4：額外約 +6.5x 時間成本

---

## 組合三：HybridCache L2 序列化

`IDistributedCache`（StackExchangeRedis）以 **Redis Hash** 儲存，每個 key 含三個欄位：

```
HSET key absexp <ticks> sldexp <ticks> data <serialized_bytes>
```

因此空間量測需使用三種方式交叉驗證：

| 量測方式 | 命令 | 說明 |
|---|---|---|
| data field 大小 | `HGET key data` 取長度 | 純序列化 bytes，不含 Redis overhead |
| MEMORY USAGE 累計 | `MEMORY USAGE key SAMPLES 0` | 含 key 名稱、Hash 結構、jemalloc 對齊 |
| **used_memory 差值** | `INFO memory` 寫入前後相減 | **Redis allocator 實際分配增量，最真實** |

### 速度

| 指標 | MessagePack | MemoryPack | 勝出 |
|---|---:|---:|---|
| 純序列化（累計） | 8.88 ms | 6.66 ms | MemoryPack ✓ |
| L2 寫入總耗時 | 209.94 ms | 192.12 ms | MemoryPack ✓ |
| 每筆平均序列化 | 0.009 ms | 0.007 ms | |
| 每筆平均 L2 寫入 | 0.210 ms | 0.192 ms | |

### Redis 空間

| 量測方式 | MessagePack | MemoryPack | 勝出 |
|---|---:|---:|---|
| data field 大小 | 225.03 KB | 359.27 KB | MessagePack ✓ |
| MEMORY USAGE 累計 | 500.01 KB | 625.01 KB | MessagePack ✓ |
| **used_memory 差值** | **606.94 KB** | **595.01 KB** | **MemoryPack ✓** |
| 每筆平均 data | 230.4 bytes | 367.9 bytes | |

### 關鍵觀察

- **data field 差 37%，但 `used_memory` 差值只差 2.1%**：Hash overhead（`absexp`、`sldexp`）與 jemalloc 對齊對兩種格式是固定成本，稀釋了序列化格式本身的差異。
- **序列化格式省了多少空間 ≠ Redis 真正省了多少空間**，應以 `used_memory` 差值為準。
- **空間反轉**：`used_memory` 差值 MemoryPack 反而略小（595 KB vs 607 KB），原因是 jemalloc 對 MessagePack 較小的 data field 對齊後，整體 overhead 反而比 MemoryPack 更高。
- **速度**：兩者差距極小，L2 寫入主要成本來自 Redis I/O，序列化差異被稀釋。

---

## 綜合建議

| 場景 | 建議格式 |
|---|---|
| Redis value 空間最小（直接寫入）| **MessagePack** |
| Redis 實際記憶體最小（HybridCache）| 以 `used_memory` 差值為準，**MemoryPack** 略優 |
| 需壓縮且資料偏大 | **MemoryPack+LZ4**（可省 ~14.5%） |
| 避免使用 | MessagePack+LZ4（小資料時反而膨脹） |
| 跨語言相容 | **MessagePack** |
