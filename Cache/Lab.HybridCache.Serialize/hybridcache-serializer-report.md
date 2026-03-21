# HybridCache L2 序列化器比較報告

## 測試條件

- 筆數：1000
- L2 後端：Redis 7 (localhost:6379)
- 模型：`ProductModel`（int、string、decimal、DateTime、List、Dictionary）
- 報告產生時間：2026-03-21 18:52:07

## 實作方式

HybridCache 的 L2 寫入流程：
```
IHybridCacheSerializer<T>.Serialize(value, bufferWriter)
  → IDistributedCache.SetAsync(key, bytes)
    → Redis HSET key absexp <ticks> sldexp <ticks> data <bytes>
```

| 格式 | 序列化器 | Redis Key 前綴 |
|---|---|---|
| HybridCache + MessagePack | `MessagePackSerializer.Serialize(bufferWriter, value)` | `hc-msgpack:{id}` |
| HybridCache + MemoryPack  | `MemoryPackSerializer.Serialize(bufferWriter, value)`  | `hc-mempack:{id}` |

## 序列化速度

| 指標 | MessagePack | MemoryPack | 勝出 |
|---|---|---|---|
| 純序列化時間（累計） | 32.71 ms | 7.99 ms | MemoryPack ✓ |
| L2 寫入總耗時（含 Redis I/O） | 255.73 ms | 200.18 ms | MemoryPack ✓ |
| 每筆平均序列化 | 0.033 ms | 0.008 ms | |
| 每筆平均 L2 寫入 | 0.256 ms | 0.200 ms | |

## Redis 空間佔用

三種量測方式說明：

| 量測方式 | 命令 | 說明 |
|---|---|---|
| data field 大小 | `HGET key data` \| len | Hash 中 `data` 欄位的純序列化 bytes，不含 Redis overhead |
| MEMORY USAGE 累計 | `MEMORY USAGE key SAMPLES 0` | 含 key 名稱、Hash 結構、jemalloc 對齊的完整記憶體 |
| used_memory 差值 | `INFO memory` 寫入前後差 | **Redis allocator 實際分配的增量**，最真實 |

| 指標 | MessagePack | MemoryPack | 勝出 |
|---|---|---|---|
| Key 數量 | 1000 | 1000 | |
| data field 大小 | 230,429 bytes（225.03 KB） | 367,893 bytes（359.27 KB） | MessagePack ✓ |
| MEMORY USAGE 累計 | 512,008 bytes（500.01 KB） | 640,008 bytes（625.01 KB） | MessagePack ✓ |
| **used_memory 差值** | **590,024 bytes（576.20 KB）** | **609,288 bytes（595.01 KB）** | **MessagePack ✓** |
| 每筆平均 data | 230.4 bytes | 367.9 bytes | |
| 每筆平均 used_memory | 590.0 bytes | 609.3 bytes | |

## 分析

### 空間
- **MessagePack** 寫入後 Redis `used_memory` 增加量較少，差距 19,264 bytes（3.2%）。
- data field 差異：137,464 bytes（37.4%），代表序列化格式本身的大小差。
- `used_memory` 差值 > `data field` 大小，因為 IDistributedCache 以 Hash 儲存（含 `absexp`、`sldexp` 欄位），加上 Redis jemalloc 對齊。

### 速度
- 純序列化：**MemoryPack** 比 MessagePack 快 24.72 ms（1000 筆累計，每筆快 0.025 ms）。
- L2 寫入總耗時以 Redis I/O 為主，序列化差異相對被稀釋。

## 建議

| 場景 | 建議 |
|---|---|
| Redis 儲存成本敏感 | 選 `used_memory` 差值較小的格式 |
| 高頻寫入、CPU 敏感 | 選純序列化較快的格式 |
| 兩者差異在容忍範圍 | 優先考量生態系（MessagePack 跨語言較佳）|
