---
title: '[ASP.NET Core] 用 IAsyncEnumerable 處理大型物件，打造低延遲、高吞吐的資料管線'
abstract: <p>在 ASP.NET Core 接收或回傳約 1MB 以上的大型 JSON 陣列時，若直接用 <code>List&lt;T&gt;</code> 接收，底層陣列只要超過 85,000 bytes 就會直接丟進大型物件堆積（Large Object Heap, LOH）。在高並發下，這會引發頻繁的 Gen2 垃圾回收（Garbage Collection, GC）、記憶體碎片化甚至直接導致 OOM。這裡透過實測排查，示範如何用 <code>ArrayPool&lt;T&gt;</code> 池化與 <code>IAsyncEnumerable&lt;T&gt;</code> 串流解析徹底避開 LOH 與 OOM 壓力。</p><figure class="image"><img style="aspect-ratio:1376/768;" src="https://dotblogsfile.blob.core.windows.net/user/余小章/5975fd56-c2d2-4ac3-9f62-cbae71717479/1788187617.jpg.jpg" width="1376" height="768"></figure>
keywords: ASP.NET Core,LOH
categories: LOH
weblogName: 余小章 @ 大內殿堂
postId: 5975fd56-c2d2-4ac3-9f62-cbae71717479
postDate: 2026-08-30T14:09:51.0000000
postStatus: 
dontInferFeaturedImage: false
stripH1Header: true
---
# [ASP.NET Core] 用 IAsyncEnumerable 處理大型物件，打造低延遲、高吞吐的資料管線

## 開發環境

- 作業系統：Ubuntu 24.04 LTS (WSL2) / Windows 11
- 開發工具：.NET 10 SDK (10.0.100)
- 程式語言：C# 14
- 效能診斷工具：dotnet-counters、dotnet-trace (Microsoft.Diagnostics.Tracing.TraceEvent 3.1.16)

---

## 1. 釐清問題核心（LOH & OOM）

.NET 的垃圾回收機制中，只要單一物件配置 $\ge 85,000\text{ bytes}$（約 83KB），CLR 就會直接配置在 LOH。

直接使用 `[FromBody] List<T>` 在高並發下容易引發 OOM，主要原因有三：  
- **記憶體碎片化（Fragmentation）**：LOH 預設不壓縮（No Compaction），回收後只留下 Free List 洞，找不到足夠連續空間就會拋出 `OutOfMemoryException`。  
- **容器記憶體硬上限（OOMKilled）**：GC 回收不等於把記憶體歸還 OS（No Decommit），多個請求連續擴容將 Working Set 灌爆，直接被 K8s / Linux Kernel 砍掉（Exit Code 137，噴掉了啦!!!）。  
- **GC 追趕不上配置速度（GC Thrashing）**：頻繁觸發 Gen2 GC 搶佔 CPU，造成請求積壓惡性循環。

---

## 2. 實作架構對照（ArrayPool vs Streaming）

針對大型集合處理，主要有兩種改善架構：

### 2.1 需隨機存取：ArrayPool 池化連續 Buffer

當業務邏輯需要完整陣列（如排序、索引存取）時，透過 `ArrayPool<T>` 租用 Buffer，並封裝成 `IDisposable` 結構在用完時歸還。

這裡我們宣告唯讀結構 `PooledArray<T>` 封裝租借陣列：

```
public readonly struct PooledArray<T> : IDisposable
{
    private readonly T[] _rented;
    public int Length { get; }

    public PooledArray(T[] rented, int length)
    {
        _rented = rented;
        Length = length;
    }

    public Span<T> Span => _rented == null ? Span<T>.Empty : _rented.AsSpan(0, Length);

    public void Dispose()
    {
        if (_rented != null)
        {
            ArrayPool<T>.Shared.Return(_rented, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
```

NOTE：若元素型別包含參考型別（如 `string` 或包含類別的 struct），`Return()` 時務必傳入 `clearArray: true`，避免記憶體洩漏。

自訂專屬的 `JsonConverter<PooledArray<T>>` 接管反序列化：

```
public sealed class PooledDoubleArrayJsonConverter : JsonConverter<PooledArray<double>>
{
    public override PooledArray<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var buffer = ArrayPool<double>.Shared.Rent(1024);
        var count = 0;
        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return new PooledArray<double>(buffer, count);
                if (count == buffer.Length)
                {
                    var newBuffer = ArrayPool<double>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, count).CopyTo(newBuffer);
                    ArrayPool<double>.Shared.Return(buffer, clearArray: false);
                    buffer = newBuffer;
                }
                buffer[count++] = reader.GetDouble();
            }
            throw new JsonException("未預期的 JSON 結尾");
        }
        catch
        {
            ArrayPool<double>.Shared.Return(buffer, clearArray: false);
            throw;
        }
    }

    public override void Write(Utf8JsonWriter writer, PooledArray<double> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var span = value.Span;
        for (var i = 0; i < span.Length; i++) writer.WriteNumberValue(span[i]);
        writer.WriteEndArray();
    }
}
```

端點中使用 `using` 確保租用陣列用完即歸還：

```
app.MapPost("/api/readings", ([FromBody] PooledArray<double> readings) =>
{
    using (readings)
    {
        var span = readings.Span;
        double sum = 0;
        for (var i = 0; i < span.Length; i++) sum += span[i];
        return Results.Ok(new ReadingsSummary(span.Length, sum, span.Length == 0 ? 0 : sum / span.Length));
    }
});
```

### 2.2 逐筆處理首選：IAsyncEnumerable 串流解析

若只需逐筆加總、過濾或寫入 DB，直接邊讀邊解，**全程 0 LOH 配置**。

這裡使用 `DeserializeAsyncEnumerable` 進行 Request 串流解析：

```
app.MapPost("/api/readings-stream", async (HttpRequest request, CancellationToken ct) =>
{
    double sum = 0;
    var count = 0;
    var stream = JsonSerializer.DeserializeAsyncEnumerable<double>(request.Body, topLevelValues: false, cancellationToken: ct);

    await foreach (var val in stream.WithCancellation(ct))
    {
        sum += val;
        count++;
    }

    return Results.Ok(new ReadingsSummary(count, sum, count == 0 ? 0 : sum / count));
});
```

---

## 3. 實測數據對照（10 並行 × 50 筆請求）

### 3.1 Request 接收 12 種組合實測

透過 .NET 10 原生 `GC.GetTotalPauseDuration()` 與 `dotnet-counters` 實測：

| 資料型別 | 實作架構 | API 端點 | 總耗時 (ms) | GC 總停頓時間 (Pause Time / 佔比) | Gen2 GC 次數 | LOH 峰值 (MB) | Working Set 記憶體 | 評語 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **1. 原生數值** *(double 4MB)* | **Streaming (串流)** | `/api/readings-stream` | **4,862** | **9.7 ms (0.36%)** | ✅ **4 次 (常規)** | **0 MB** | **112 MB** | 🏆 **0 LOH、停頓極短** |
|  | **ArrayPool (池化)** | `/api/readings` | 3,692 | 53.7 ms (0.32%) | ⚠️ 10 次 (常規) | 2 MB | 220 MB | ⚡ 連續 Buffer 租借複用 |
|  | **List (未池化)** | `/api/readings-list` | 2,929 | 13.9 ms (0.26%) | ⚠️ 5 次 (常規) | 2 MB | 223 MB | ❌ 4MB 大陣列直接衝進 LOH |
| **2. 原生字串** *(string 50k 筆)* | **Streaming (串流)** | `/api/strings-stream` | **1,938** | **18.4 ms (0.26%)** | ✅ **1 次 (常規)** | **2 MB** | **222 MB** | 🏆 **字串最佳解，0 LOH** |
|  | **ArrayPool (池化)** | `/api/strings` | 2,208 | 44.9 ms (0.31%) | ❌ **2 次 (劇烈)** | 2 MB | 221 MB | ⚠️ 僅池化指標，字串仍在 Gen0 |
|  | **List (未池化)** | `/api/strings-list` | 2,150 | **102.2 ms (0.45%)** | ❌ **2 次 (劇烈)** | 2 MB | 225 MB | ❌ 擴容指標衝破 85KB LOH |
| **3. 巢狀結構** *(Struct 20k 筆)* | **Streaming (串流)** | `/api/members-stream` | **2,783** | **23.1 ms (0.42%)** | ✅ **1 次 (常規)** | **2 MB** | **224 MB** | 🏆 **Struct 最佳解，停頓最低** |
|  | **ArrayPool (池化)** | `/api/members` | 2,295 | 28.1 ms (0.42%) | ⚠️ 1 次 (常規) | 2 MB | 223 MB | ⚡ 資料內嵌於連續 Buffer |
|  | **List (未池化)** | `/api/members-list` | 2,318 | **144.9 ms (0.56%)** | ❌ **3 次 (劇烈)** | 2 MB | 221 MB | ❌ 頻繁觸發 Gen2 Full GC |
| **4. 參考型別** *(Class 20k 筆)* | **Streaming (串流)** | `/api/members-class-stream` | **2,294** | **36.1 ms (0.55%)** | ✅ **1 次 (常規)** | **2 MB** | **223 MB** | 🏆 **Class 最佳解，停頓降 66%** |
|  | **ArrayPool (池化)** | `/api/members-class-pooled` | 2,332 | 90.8 ms (0.60%) | ❌ **1 次 (劇烈)** | 2 MB | 223 MB | ⚠️ 池化效益低，物件觸發 GC |
|  | **List (未池化)** | `/api/members-class-list` | 2,406 | **179.1 ms (0.72%)** | ❌ **2 次 (劇烈)** | 2 MB | 223 MB | ❌ 4 萬個 Class 實體散落 Gen0 |

### 3.2 Response 回傳與 Client 接收重點

1. **Response 回傳（12 組實測）**：回傳大型資料時，`IAsyncEnumerable<T>` 串流回傳全面制霸（耗時 304~787ms，GC 停頓時間大幅降低，全程 0 LOH）。
2. **Client 端 0 LOH 接收**：Client 端若直接用 `GetFromJsonAsync<List<T>>()`，會造成 Client 端 LOH 飆升；正確寫法應配合 `ResponseHeadersRead` 與 `DeserializeAsyncEnumerable` 串流消費：

```
// Client 端 0 LOH 接收
using var request = new HttpRequestMessage(HttpMethod.Get, "/api/export-stream");
using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
using var stream = await response.Content.ReadAsStreamAsync(ct);

await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<MemberAccount>(stream, options, ct))
{
    Process(item); // 記憶體中永遠只有一筆
}
```

1. **量測工具選擇**：
2. 抓 **LOH 世代精確配置與短命垃圾** $\rightarrow$ 首選 `GC.GetGCMemoryInfo()`（In-Process）。
3. 抓 **K8s 容器記憶體硬上限與 OOMKilled** $\rightarrow$ 首選 `dotnet-counters`（Out-of-Process）。

---

## 4. 生產環境選型決策（SOP）

```
接收大型 JSON 陣列 (≥ 85KB)
├── 步驟 1：業務邏輯需要拿到完整陣列才能運算嗎？
│   ├── 【否】👉 唯一首選：IAsyncEnumerable<T> 串流解析 (S 級，0 LOH、停頓極短)
│   └── 【是】👉 步驟 2：資料模型可以設計成 struct 嗎？
│       ├── 【能】👉 選擇：readonly struct + ArrayPool (A 級，完整池化)
│       └── 【不能（既有 class）】👉 強制重構為串流處理，切勿直接用 List<T>
```

---

## 5. 重現實驗腳本

專案內建一鍵壓測與報表輸出工具：

```
# 1. 一鍵執行全套 32 組壓測（Server 24組 + Client 8組）
./scripts/benchmark-all.sh

# 2. 秒級輸出彙總 Markdown 報表
./scripts/benchmark-all.sh --report
```

---

## 心得

- **串流解析（IAsyncEnumerable）為第一首選**：只要不需隨機存取，Request 接收與 Response 回傳一律採用串流，能達成 0 LOH 並大幅壓低 GC 停頓時間。
- **ArrayPool 池化適用於 Struct / 原生數值**：資料內嵌於 Buffer 能發揮最大效益；若為 Class 參考型別，僅池化指標陣列效益有限。
- **避免使用 List 接收大型資料**：擴容機制會產生大量短命大陣列衝入 LOH，引發 Gen2 GC 停頓與 OOMKilled 風險。

---

## 範例位置

完整代碼位置：<https://github.com/yaochangyu/sample.dotblog/tree/master/WebAPI/Lab.LargeObject>
