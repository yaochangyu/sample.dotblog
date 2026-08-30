# [.NET] ASP.NET Core 接收大型 JSON 集合：從 ArrayPool 池化到 IAsyncEnumerable 串流解析徹底避開 LOH 與 OOM 實戰

在 ASP.NET Core 接收或回傳約 1MB 以上的大型 JSON 陣列時，若直接用 `List<T>` 接收，底層陣列只要超過 85,000 bytes 就會直接丟進大型物件堆積 (Large Object Heap, LOH)。在高並發下，這會引發頻繁的 Gen2 垃圾回收 (Garbage Collection, GC) 與記憶體碎片化。這裡透過實測排查，示範如何用 `ArrayPool<T>` 池化與 `IAsyncEnumerable<T>` 串流解析徹底避開 LOH 壓力。

## 開發環境

- 作業系統：Ubuntu 24.04 LTS (WSL2) / Windows 11
- 開發工具：.NET 10 SDK (10.0.100)
- 程式語言：C# 14
- 效能診斷工具：dotnet-counters、dotnet-trace (Microsoft.Diagnostics.Tracing.TraceEvent 3.1.16)

---

## 1. 釐清問題核心（LOH & OOM）

.NET 的垃圾回收機制 (GC) 中有一個固定規則：只要單一物件配置的大小大於或等於 85,000 bytes（約 83KB），CLR 就會直接將其配置在大型物件堆積 (Large Object Heap, LOH)。

這裡先釐清兩個關鍵概念：
1. **陣列容器 vs 複雜物件圖**：如果一個 1MB 的 JSON 是一大包物件清單，底層反序列化出來的陣列容器本身就是一塊超過 85,000 bytes 的連續記憶體，一定會進 LOH；但如果是由許多小物件組成的樹狀結構，只要每個單一物件小於門檻，其實全部都在 Gen0。
2. **LOH 回收不等於將實體記憶體還給作業系統**：LOH 上的垃圾只有在 Gen2 Full GC 時才會被清理，且預設不會進行記憶體壓縮 (Compaction)，只會留下自由列表 (Free List) 供後續重複使用。這代表工作集 (Working Set) 漲上去之後不會自動縮小。

未池化的大物件（如 `[FromBody] List<T>`）之所以會在高並發下引發記憶體不足 (Out of Memory, OOM)，主要來自三個途徑：
- **記憶體碎片化 (Fragmentation)**：LOH 預設不壓縮，回收後留下的洞若無法容納新的連續大陣列，就算總可用記憶體足夠，依然會拋出 `OutOfMemoryException`。
- **容器記憶體硬上限 (OOMKilled)**：多個請求同時擴容大陣列，實體記憶體飆升撞上 Kubernetes Pod 的 `limits.memory`，直接被 Linux Kernel 砍掉 (Exit Code 137)。
- **GC 追趕不上配置速度 (GC Thrashing)**：頻繁觸發 Gen2 GC 搶佔 CPU 運算資源，API 處理變慢造成請求在記憶體中積壓，形成惡性循環。

---

## 2. 實作池化架構（ArrayPool & JsonConverter）

若端點的業務邏輯必須取得完整陣列進行隨機存取或排序，最直接的改善方式是使用 `ArrayPool<T>` 租借連續 Buffer，取代每次請求都 `new` 出新的大陣列。

### 2.1 封裝租借陣列（PooledArray<T>）

這裡我們宣告一個唯讀結構 `PooledArray<T>`，包裝自 `ArrayPool<T>.Shared` 租借出來的陣列，並實作 `IDisposable` 以便在端點中使用 `using` 確保歸還：

```csharp
namespace Lab.LargeObject.Api;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

public readonly struct PooledArray<T> : IDisposable
{
    private readonly T[] _rented;
    public int Length { get; }

    public PooledArray(T[] rented, int length)
    {
        _rented = rented;
        Length = length;
    }

    public ReadOnlySpan<T> Span => _rented.AsSpan(0, Length);

    public void Dispose()
    {
        if (_rented is not null)
        {
            ArrayPool<T>.Shared.Return(_rented, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
```

NOTE：租借出來的陣列長度通常會大於請求的實際資料筆數（因為 ArrayPool 會向上對齊到桶子大小），所以必須透過 `Length` 屬性與 `Span` 來界定有效範圍；離開 `using` 區塊前千萬不能讓租借的陣列參考外流。

### 2.2 宣告資料模型（struct vs class）

當陣列元素是複合資料時，元素型別必須設計為 `readonly struct`。如果是 `class`，在 64 位元環境下陣列裡存放的只是 8 bytes 的指標，當筆數超過 10,625 筆時，指標陣列本身會因為超過 85,000 bytes 進入 LOH，但物件實體依然散落在 Gen0 Heap 上。`ArrayPool<T>` 僅能池化指標陣列，無法避免數萬個 Class 物件造成的 GC 負擔。

這裡我們定義巢狀會員帳號的結構：

```csharp
public readonly struct ContactInfo
{
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public readonly struct MemberAccount
{
    public required long MemberId { get; init; }
    public required string Account { get; init; }
    public required string DisplayName { get; init; }
    public required MemberStatus Status { get; init; }
    public required ContactInfo Contact { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### 2.3 自訂陣列池化 JsonConverter（JsonSerializer.Deserialize）

在實作 `JsonConverter<PooledArray<MemberAccount>>` 時，我們只需接管最外層「陣列容器」的租借與擴容，而內部的單一物件元素直接交由 `JsonSerializer.Deserialize<MemberAccount>(ref reader, options)` 遞迴解析即可，不需要手刻繁瑣的逐欄位對應代碼：

```csharp
public sealed class PooledMemberAccountArrayJsonConverter : JsonConverter<PooledArray<MemberAccount>>
{
    public override PooledArray<MemberAccount> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("預期為 JSON 陣列開頭");
        }

        var capacity = 256;
        var rented = ArrayPool<MemberAccount>.Shared.Rent(capacity);
        var count = 0;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == rented.Length)
                {
                    var newRented = ArrayPool<MemberAccount>.Shared.Rent(rented.Length * 2);
                    Array.Copy(rented, newRented, count);
                    ArrayPool<MemberAccount>.Shared.Return(rented, clearArray: true);
                    rented = newRented;
                }

                // 元素直接交由 JsonSerializer.Deserialize 反序列化
                rented[count++] = JsonSerializer.Deserialize<MemberAccount>(ref reader, options);
            }

            return new PooledArray<MemberAccount>(rented, count);
        }
        catch
        {
            ArrayPool<MemberAccount>.Shared.Return(rented, clearArray: true);
            throw;
        }
    }

    public override void Write(Utf8JsonWriter writer, PooledArray<MemberAccount> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var span = value.Span;
        for (var i = 0; i < span.Length; i++)
        {
            JsonSerializer.Serialize(writer, span[i], options);
        }
        writer.WriteEndArray();
    }
}
```

NOTE：這種寫法足以解決 90% 以上的 LOH 與 OOM 問題。只有在極端高吞吐量下，若 Profiler（如 dotnet-trace）抓出 `required` 屬性在 STJ 內部產生了 `ArgumentState` 追蹤成本且構成瓶頸時，才需進一步評估手刻底層 `Utf8JsonReader` + `ValueTextEquals`。

---

## 3. 實作串流解析（IAsyncEnumerable）

如果業務邏輯不需要隨機存取，只需要逐筆累加、過濾或批次寫入資料庫，那麼 `IAsyncEnumerable<T>` 串流反序列化是更理想的解法。

### 3.1 接收串流請求（Request Streaming）

這裡我們透過 `JsonSerializer.DeserializeAsyncEnumerable<T>(request.Body)` 直接從 HTTP Request 串流中邊讀取邊解析，記憶體中永遠只保留當前處理的一筆資料：

```csharp
app.MapPost("/api/members-stream", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    var count = 0;
    var active = 0;
    var suspended = 0;
    var deleted = 0;

    await foreach (var member in JsonSerializer.DeserializeAsyncEnumerable<MemberAccount>(
                       request.Body, serializerOptions, cancellationToken: cancellationToken))
    {
        count++;
        switch (member.Status)
        {
            case MemberStatus.Active:
                active++;
                break;
            case MemberStatus.Suspended:
                suspended++;
                break;
            case MemberStatus.Deleted:
                deleted++;
                break;
        }
    }

    return Results.Ok(new MemberAccountSummary(count, active, suspended, deleted));
});
```

### 3.2 回傳與客戶端串流消費（Response & HttpClient）

回傳大量資料時，端點同樣可以使用 `async IAsyncEnumerable<T>` 產出資料；而在 Client 端調用時，必須指定 `HttpCompletionOption.ResponseHeadersRead`，避免 `HttpClient` 將整個 Body 快取到記憶體中：

```csharp
// Client 端 0 LOH 接收範例
using var request = new HttpRequestMessage(HttpMethod.Get, "/api/export-members-stream");
using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

using var stream = await response.Content.ReadAsStreamAsync(ct);
await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<MemberAccount>(stream, options, ct))
{
    // 逐筆消費，記憶體不堆積大陣列
    Process(item);
}
```

---

## 4. 排查觀測與驗證假設（dotnet-trace）

這次排查過程中，工具換了三次：

1. **自建 `/diag/gc-stats` 端點**：透過 `GC.GetGCMemoryInfo()` 與 `GC.GetTotalPauseDuration()` 讀取 CLR 內部資訊，能看到各世代 (Gen0/1/2) 的觸發次數與精確停頓時間，適合在整合測試中做精確斷言。
2. **dotnet-counters 命令列監控**：雖然可以非侵入式監控即時 Working Set 與 GC 次數，但其預設取樣週期為 1 秒。當短命的大陣列在 1 秒內被建立又迅速被回收時，`dotnet-counters` 容易遺漏瞬時的 LOH 尖峰。
3. **dotnet-trace 與 TraceEvent 事件分析**：透過 `dotnet-trace collect --profile gc-verbose` 收集 `.nettrace`，並寫小工具訂閱 `ClrTraceEventParser.GCAllocationTick` 事件，能精確捕捉到每一筆 LOH（`AllocationKind.Large`）配置發生的毫秒時間戳與大小。

### 4.1 驗證與推翻假設（ThreadPool vs Cache Miss）

在連續發送請求時，曾觀察到 LOH 大小呈現階梯狀上升，一度推測是 ThreadPool 新增 Worker Thread 導致。後來透過 `dotnet-trace` 分析配置事件時間點，發現 500 次請求中只有前 8 次發生了陣列倍增配置，其餘 492 次（98.4%）完全零配置。這證明了階梯上升純粹是「ArrayPool 桶子暖機」過程，而非 ThreadPool 膨脹。

### 4.2 世代晉升與 Budget-driven 回收機制

在 .NET CLR 中，Gen2 GC 不是定時觸發的，而是「動態預算（Budget-driven）」機制：
- 當大量資料在非同步網路 I/O 等待時，暫存的微型狀態物件自然晉升至 Gen1 與 Gen2，填滿了 Gen2 的 Budget 配額，因而觸發了常規的 Gen2 GC。
- 關鍵在於：**沒有 LOH 大垃圾時，Gen2 GC 每次回收只需 0.5ms，停頓時間極短；但若有短命 LOH 大物件，Gen2 GC 停頓時間會高達數百毫秒**。

---

## 5. 比較實測數據與技術選型（Benchmark & Decision）

在相同硬體環境下（10 個並行請求、共 50 筆 1MB~4MB 請求），針對 4 種資料型別與 3 種架構進行全面壓測，各世代 GC 次數與推薦等級彙總如下：

### 5.1 評估請求端實測數據（Request Benchmark）

| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時 (ms) | GC 總停頓時間 (Pause Time) | Gen0 GC 次數 | Gen1 GC 次數 | Gen2 GC 次數 | LOH 峰值 (MB) | Working Set 實體記憶體 | 核心評語與行為特徵 |
|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| **S 級** | **1. 原生數值**<br>*(double 4MB)* | **Streaming (串流)** | `/api/readings-stream` | **4,862** | **9.7 ms (0.36%)** | **4 次** | **4 次** | **4 次 (常規)** | **0 MB** | **112 MB** | 0 LOH、停頓極短、邊收邊算 |
| **A 級** | **1. 原生數值**<br>*(double 4MB)* | **ArrayPool (池化)** | `/api/readings` | 3,692 | 53.7 ms (0.32%) | 10 次 | 10 次 | 10 次 (常規) | 2 MB | 220 MB | 連續 Buffer 租借歸還，暖機後穩定 |
| **D 級** | **1. 原生數值**<br>*(double 4MB)* | **List (未池化)** | `/api/readings-list` | 2,929 | 13.9 ms (0.26%) | 5 次 | 5 次 | 5 次 (常規) | 2 MB | 223 MB | 連續短命 4MB 陣列砸進 LOH |
| **S 級** | **2. 原生字串**<br>*(string 50,000 筆)* | **Streaming (串流)** | `/api/strings-stream` | **1,938** | **18.4 ms (0.26%)** | 24 次 | **2 次** | **1 次 (常規)** | **2 MB** | **222 MB** | 字串最佳解，0 LOH、停頓極短 |
| **C 級** | **2. 原生字串**<br>*(string 50,000 筆)* | **ArrayPool (池化)** | `/api/strings` | 2,208 | 44.9 ms (0.31%) | 13 次 | 8 次 | 2 次 (劇烈) | 2 MB | 221 MB | 僅池化指標陣列，字串實體散落 Gen0 |
| **D 級** | **2. 原生字串**<br>*(string 50,000 筆)* | **List (未池化)** | `/api/strings-list` | 2,150 | 102.2 ms (0.45%) | 12 次 | 6 次 | 2 次 (劇烈) | 2 MB | 225 MB | 擴容指標陣列衝破 85KB LOH |
| **S 級** | **3. 巢狀結構**<br>*(Struct 20,000 筆)* | **Streaming (串流)** | `/api/members-stream` | **2,783** | **23.1 ms (0.42%)** | 11 次 | **2 次** | **1 次 (常規)** | **2 MB** | **224 MB** | Struct 最佳解，停頓最低、0 LOH |
| **A 級** | **3. 巢狀結構**<br>*(Struct 20,000 筆)* | **ArrayPool (池化)** | `/api/members` | 2,295 | 28.1 ms (0.42%) | 7 次 | 4 次 | 1 次 (常規) | 2 MB | 223 MB | 資料內嵌於連續 Buffer，隨機存取首選 |
| **D 級** | **3. 巢狀結構**<br>*(Struct 20,000 筆)* | **List (未池化)** | `/api/members-list` | 2,318 | 144.9 ms (0.56%) | 13 次 | 6 次 | 3 次 (劇烈) | 2 MB | 221 MB | 頻繁觸發 Gen2 Full GC |
| **B 級** | **4. 參考型別**<br>*(Class 20,000 筆)* | **Streaming (串流)** | `/api/members-class-stream` | **2,294** | **36.1 ms (0.55%)** | 10 次 | **2 次** | **1 次 (常規)** | **2 MB** | **223 MB** | Class 最佳解，GC 停頓降 66% |
| **C 級** | **4. 參考型別**<br>*(Class 20,000 筆)* | **ArrayPool (池化)** | `/api/members-class-pooled` | 2,332 | 90.8 ms (0.60%) | 8 次 | 5 次 | 1 次 (劇烈) | 2 MB | 223 MB | 池化效益低，物件依舊觸發 GC |
| **D 級** | **4. 參考型別**<br>*(Class 20,000 筆)* | **List (未池化)** | `/api/members-class-list` | 2,406 | 179.1 ms (0.72%) | 11 次 | 7 次 | 2 次 (劇烈) | 2 MB | 223 MB | 4 萬個 Class 實體散落 Gen0 |

### 5.2 選擇生產環境架構（Decision Tree）

根據實測與架構特性，可歸納出以下選型決策：
1. **不需要隨機存取時**：一律優先選擇 `IAsyncEnumerable<T>` 串流解析。不論是數值、字串、Struct 還是既有的 Class 模型，都能達成最低的記憶體佔用與極短的 GC 停頓時間。
2. **必須隨機存取或整包處理時**：
   - 資料模型應盡可能宣告為 `readonly struct`，搭配 `ArrayPool<T>` 與自訂 `JsonConverter`。
   - 避免在 `ArrayPool` 中使用 `class` 或純 `string` 集合，因為池化指標陣列對物件/字串實體配置沒有節省效果。

---

## 6. 整合測試與自動化壓測工具（Tests & Scripts）

為了確保高並發與串流處理在生產環境中的穩定性，專案中實作了完整的自動化測試與跨場景壓測腳本。

### 6.1 單元與整合測試（Tests & HttpClient 串流擴充）

在測試專案 `Lab.LargeObject.Api.Tests` 中，我們透過 `WebApplicationFactory<Program>` 驗證所有端點功能。為了讓 Client 端也能享有 0 LOH 的串流消費，我們封裝了 `GetFromJsonStreamingAsync<T>` 擴充方法：

```csharp
public static class HttpClientStreamingExtensions
{
    public static async IAsyncEnumerable<T> GetFromJsonStreamingAsync<T>(
        this HttpClient client,
        string requestUri,
        JsonSerializerOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // 關鍵 1：必須指定 ResponseHeadersRead，避免 HttpClient 緩衝 Body
        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        // 關鍵 2：透過 Stream 與 DeserializeAsyncEnumerable 逐筆解析
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
            stream, options, cancellationToken: cancellationToken).WithCancellation(cancellationToken))
        {
            if (item is not null) yield return item;
        }
    }
}
```

在整合測試 `HttpClientStreamingTests` 中，我們更進一步利用 `GC.GetGCMemoryInfo()` 對 Client 端在接收 524,288 筆資料時的 LOH 配置進行精確斷言，直接證明了未池化 `List<double>` 會在 Client 端產生數十 MB 的 LOH 垃圾，而 `IAsyncEnumerable<double>` 則維持 0 LOH。

### 6.2 自動化壓測腳本（benchmark-all.sh & BenchClient）

專案在 `scripts/` 目錄下提供了一鍵執行的 Bash 壓測工具，支援 32 組全場景壓測（Server 端 24 組 + Client 端 8 組）：

```bash
# 🚀 1. 【全套總指揮】一鍵重跑全套 32 組壓測（Server 24組 + Client 8組）
./scripts/benchmark-all.sh

# ⚡ 2. 【秒級報表】輸出 32 組大一統 Markdown 彙總大表（快取重用，無需重跑）
./scripts/benchmark-all.sh --report

# 🖥️ 3. 【Server 專題】執行 Server 端 24 組壓測
./scripts/benchmark-server.sh             # 跑 Server 全套 24 組
./scripts/benchmark-server.sh --request   # 僅跑 Request 12 組
./scripts/benchmark-server.sh --response  # 僅跑 Response 12 組

# 💻 4. 【Client 專題】執行 8 組 Client 端實測與量測工具對照
./scripts/benchmark-client.sh
```

腳本會自動啟動 API 伺服器，透過 `dotnet-counters` 與自研的 `BenchClient` 壓測工具發送並行請求，記錄耗時、GC 次數與記憶體峰值，並將結果持久化至檔案中，方便持續比對不同版本的效能變化。

---

## 心得

- `IAsyncEnumerable<T>` 串流解析在記憶體控制上表現最為優異，全程不產生 LOH 陣列容器，是處理批次匯入或大量匯出 API 的首選方案。
- `ArrayPool<T>` 適合用於必須整包載入記憶體運算的場景，但務必搭配 `struct` 才能發揮完整的連續記憶體池化優勢。
- 效能調優必須依賴正確的工具；單看外部取樣工具容易被平均值或取樣間隔誤導，結合 Process 內的 `GC.GetGCMemoryInfo()` 與 `dotnet-trace` 事件才能看清記憶體配置的真實全貌。

完整代碼位置: https://github.com/yc421206/sample.dotblog/tree/main/WebAPI/Lab.LargeObject
