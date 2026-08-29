# LOH 排查實驗記錄

這份文件記錄一次完整的排查過程：從「Grafana 看不出記憶體變化」的疑問開始，一路做到用 `dotnet-trace` 抓出真正的配置事件為止。刻意保留中間**推翻掉的假設**，因為排查過程中「為什麼錯」往往比「最後對在哪」更有參考價值。

## 目錄

1. [緣起](#1-緣起)
2. [情境判斷：陣列容器 vs 物件圖](#2-情境判斷陣列容器-vs-物件圖)
3. [正確寫法：ArrayPool + 自訂 JsonConverter](#3-正確寫法array pool--自訂-jsonconverter)
4. [觀察工具的演進：自建端點 → dotnet-counters → dotnet-trace](#4-觀察工具的演進自建端點--dotnet-counters--dotnet-trace)
5. [LOH 會不會封頂？一路加測試次數的實驗記錄](#5-loh-會不會封頂一路加測試次數的實驗記錄)
6. [用 dotnet-trace 找到真正原因](#6-用-dotnet-trace-找到真正原因)
7. [附帶發現：反射式反序列化的隱藏成本](#7-附帶發現反射式反序列化的隱藏成本)
8. [反序列化配置量的最終解法：手刻 Utf8JsonReader 解析](#8-反序列化配置量的最終解法手刻-utf8jsonreader-解析)
9. [結論](#9-結論)
10. [附錄：如何重現](#10-附錄如何重現)

---

## 1. 緣起

起點是一個實務問題：ASP.NET Core 部署在 k8s，端點接收約 1MB 的 JSON body，懷疑有大物件，但 Grafana 上完全看不出記憶體有明顯變化。

先釐清兩個基礎概念：

- **LOH 門檻**：.NET 物件只要**單一物件** ≥ 85,000 bytes，就一定配置在 Large Object Heap（LOH），這是 CLR 寫死的規則，沒有例外。
- **LOH 回收 ≠ 記憶體還給 OS**：物件變成不可達後，要等對應的 gen2 GC 才會被標記回收；回收後的空間預設只是變成 free list 留在原地重複利用，不會馬上 decommit 還給作業系統。這也是「Grafana 看不出變化」的部分原因——記憶體漲上去之後本來就不會自己降回來，反而更難用「趨勢」去判斷有沒有異常。

## 2. 情境判斷：陣列容器 vs 物件圖

「1MB 的強型別物件」可以是兩種完全不同的東西，兩者對 LOH 的影響天差地遠：

- **情境 A（單一大陣列）**：例如一個 `double[131072]`，陣列本身就是一塊連續記憶體，整包超過門檻，**保證**進 LOH。
- **情境 B（複雜物件圖）**：巢狀很多小欄位/小物件，每個都遠低於 85,000 bytes，加總才到 1MB。這種情況下**沒有任何單一物件**會進 LOH，全部落在 gen0，跟 LOH 完全無關。

這次排查最後鎖定在情境 A：一個會員帳號批次匯入端點，接收的是一個大陣列（陣列元素是巢狀的強型別物件）。

## 3. 正確寫法：ArrayPool + 自訂 JsonConverter

### 3.1 為什麼不能直接綁 `[FromBody] T[]`

`System.Text.Json` 預設反序列化陣列會用 `new T[]`，這個陣列物件本身就會落在 LOH。每個 request 都製造一個新的、用完立刻變垃圾的 LOH 物件，會逼 GC 頻繁介入（實測見附錄的 naive vs pooled 對照）。

### 3.2 核心模式：`PooledArray<T>` + 專屬的 `JsonConverter`

```csharp
public readonly struct PooledArray<T> : IDisposable
{
    private readonly T[] _rented;
    public int Length { get; }
    public ReadOnlySpan<T> Span => _rented.AsSpan(0, Length);

    public void Dispose() =>
        ArrayPool<T>.Shared.Return(_rented, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
}
```

- `ArrayPool<T>.Shared.Rent()` 租陣列取代 `new T[]`，不夠大時倍增租用、歸還舊的。
- 租出來的陣列長度**不等於**實際資料長度（會被無條件捨入到 bucket 大小），所以額外記錄 `Length`。
- 端點寫法把使用範圍鎖死在 `using (readings) { ... }` 裡，離開這個範圍前不能讓 `PooledArray<T>` 外流——歸還之後，池子隨時可能把同一塊記憶體租給別的並行 request，外流的參考會變成難以重現的資料錯亂 bug。

### 3.3 陣列元素是巢狀物件時：`struct`，不能是 `class`

用「會員帳號」網域驗證了這個模式在巢狀複雜型別上依然適用，但有個容易忽略的前提：

- **元素型別必須是 `struct`**。如果是 `class`，陣列裡存的只是參考（指標），個別物件實體還是各自獨立配置在 heap 上，`ArrayPool<T>` 頂多幫你少配置那些指標，真正佔大小的物件本體完全沒被池化，等於白做。
- **只有陣列容器需要手動接管**，巢狀欄位交給 `JsonSerializer.Deserialize<T>(ref reader, options)` 讓 STJ 遞迴處理——不需要、也不應該手刻逐欄位解析。

```csharp
public readonly struct ContactInfo { public required string Email { get; init; } public string? PhoneNumber { get; init; } }
public readonly struct MemberAccount { public required long MemberId { get; init; } /* ... */ public required ContactInfo Contact { get; init; } }
```

實測 `Unsafe.SizeOf<MemberAccount>() = 64 bytes`。20,000 筆的陣列容器 ≈ 1.22MB，是門檻的 15.1 倍，用來當測試 payload 確保穩定跨過 LOH 門檻。

## 4. 觀察工具的演進：自建端點 → dotnet-counters → dotnet-trace

排查過程中觀察工具換了三次，每次都是因為上一個工具的解析度不夠：

| 階段 | 工具 | 能看到什麼 | 侷限 |
|---|---|---|---|
| 1 | 自建 `/diag/gc` 端點（`GC.GetGCMemoryInfo()` + `Process.WorkingSet64`） | 各世代大小、GC 次數、working set | 只能看「當下快照」，要自己寫程式碼維護、不是標準做法 |
| 2 | `dotnet-counters collect`（`System.Runtime` provider） | 官方標準指標，1 秒取樣一次，含 LOH 大小、gen2 GC 頻率、working set | **取樣頻率不夠**：多個配置事件如果發生在同一秒內，會被合併看成一次跳躍，看不出真正的時間分布；而且某些計數器（如 `thread_pool.thread.count`）是 rate 型態，容易誤讀成絕對值（見 5.4 節的教訓） |
| 3 | `dotnet-trace collect --profile gc-verbose` + 自寫的 TraceEvent 分析器 | **每一次** LOH 配置事件的精確時間（毫秒級）、型別、大小 | 需要額外寫一支小工具解析 `.nettrace`（`dotnet-trace` 本身沒有現成的事件明細輸出） |

第 3 階段的分析器是一支獨立的 console 專案，用 `Microsoft.Diagnostics.Tracing.TraceEvent` NuGet 套件讀 `.nettrace`，訂閱 `ClrTraceEventParser.GCAllocationTick` 事件：LOH（`AllocationKind.Large`）的配置**每次都會記錄**、精確到毫秒；一般 gen0 物件（`AllocationKind.Small`）則是每累積約 100KB 抽樣一次，只能看相對比例，不是精確次數。

## 5. LOH 會不會封頂？一路加測試次數的實驗記錄

用 `dotnet-counters` 對同一個端點（相同大小的 payload）連續 POST，觀察 LOH 大小是否會無限成長：

| 請求次數 | 耗時 | LOH 最終 plateau | gen2 GC 總次數 | Working Set |
|---|---|---|---|---|
| 3 | — | 8,127,024 bytes（~8.1MB） | — | — |
| 20 | ~3s | 24,381,072 bytes（~24.4MB） | 10 | 83MB → 213MB |
| 100 | 6s | 24,381,072 bytes（**跟 20 次完全一樣**） | 13 | 226.6MB |
| 500 | 22s | 32,606,480 bytes（~32.6MB，分階梯跳升） | 14 | 258MB |
| 2000 | 81s | 40,635,120 bytes（~40.6MB，分階梯跳升） | 14 | — |

觀察到的關鍵現象：

- **LOH 每次測試規模加大都可能再墊高一階，但每一輪測試裡最終都會趨於平坦**，即使拉到 2000 次、跑 81 秒也一樣不會無限成長。
- **gen2 GC 總次數幾乎不隨請求量增加**（13 → 14 → 14），代表大部分請求根本沒有觸發新的 GC 壓力。
- Working Set 一旦漲上去，就不會自動降回來（呼應第 1 節提到的「回收不等於還給 OS」）。

### 5.1 假設一：thread pool 持續成長（後來被推翻）

20 次跟 100 次的 LOH 數字完全相同，一開始推測是「ASP.NET Core ThreadPool 逐步注入新 worker thread，每條新 thread 第一次用到某個 bucket 大小時才會真的配置新記憶體」。

500 次、2000 次測試規模加大後，這個假設看似被印證（LOH 又墊高了）——但這只是**表面上時間點相符**，並沒有真的量到 thread 數量本身。

### 5.2 驗證假設一：抓錯了指標型態

用 `dotnet-counters` 追加 `dotnet.thread_pool.thread.count`，想直接看 thread 數量是否真的隨時間增加。

第一次直接讀 CSV 裡的數值，看到大多是 `0`，偶爾 `-1`、`-4`，一度誤以為「這個指標壞了」或「需要換工具」。

**釐清問題所在**：`dotnet-counters collect` 對這個計數器輸出的是「**每秒的淨變化量（delta/rate）**」，不是「目前絕對值」。修正方式不需要重新收集資料，直接對已經拿到的 CSV 做累計加總：

```bash
awk -F',' '{sum+=$2; print sum}' thread_delta_series.csv
```

結果：**整段測試期間，thread 數量的累計淨變化是 0**。

### 5.3 假設一正式推翻

thread pool 淨變化為 0，直接跟「持續注入新 thread」的假設矛盾。這個解釋被撤回——這是排查中很重要的一步：**與其讓一個「聽起來合理」的假設留在結論裡，不如老實承認「目前不知道真正原因」，直到有更直接的證據**。

## 6. 用 dotnet-trace 找到真正原因

用 `dotnet-trace collect --profile gc-verbose` 抓 500 次 POST 期間的每一筆 GC 配置事件，寫小工具解析 `GCAllocationTick`，直接看「LOH 陣列到底被配置了幾次、什麼時候」。

### 6.1 結果：500 次裡只有 8 次真的配置了新的 LOH 陣列

```
131,096 → 262,168 → 524,312 → 1,048,600 → 2,097,176 bytes
（對應租用陣列從 2048 → 4096 → 8192 → 16384 → 32768 個元素，逐步倍增）
```

8 次「倍增鏈」總共配置 ≈ 31.8MB，跟 `dotnet-counters` 量到的最終 plateau（32.6MB）幾乎完全對上——**兩組獨立工具、獨立量測方式互相印證**。

換句話說：**500 次請求裡，492 次（98.4%）完全零新配置**，純粹重複利用池子裡已經存在的 buffer。

### 6.2 時間分布：高度集中在暖機階段

8 次配置裡，**7 次集中在頭 5.3 秒**（測試最開始的幾十個請求內），接著有將近 **14.5 秒完全沒有任何新配置**，最後在第 19.7 秒附近再出現 1 次，直到 22 秒測試結束都沒再發生。

這解釋了 `dotnet-counters` 看到的「階梯狀成長」：不是持續成長，而是極少數的暖機期配置事件，被 1 秒取樣頻率的 `dotnet-counters` 合併/放大成看起來像連續的跳躍。用毫秒級的 `dotnet-trace` 一看，真相是「早期集中幾次、中間長時間完全平靜」。

### 6.3 修正後的結論

**LOH 會封頂，機制是「池子暖機」，不是 thread pool 持續成長。** 少數幾次因為對應大小的 buffer 還沒被建立過（cache miss）而配置新記憶體，一旦幾個常用大小的 bucket 都被填過一次，後面上百上千次一模一樣大小的請求就只是租用/歸還，不會再製造新的 LOH 壓力。

## 7. 附帶發現：反射式反序列化的隱藏成本

用來抓 LOH 事件的同一份 trace，順便攤開了整個 request 處理過程中「誰配置最多」，結果 LOH 陣列本身完全不是大頭：

| 型別 | 配置量（估計） | 說明 |
|---|---|---|
| `System.Text.Json.ReadStackFrame[]` | ~4.1GB | STJ **反射式**反序列化的內部機制 |
| `System.String` | ~2.2GB | 屬性名稱/字串值 |
| `Lab.LargeObject.Api.MemberAccount`（裝箱） | ~924MB | struct 透過反射建構時被 boxing |
| `System.Byte[]` | ~840MB | 讀取緩衝區 |
| `System.Collections.BitArray` | ~751MB | STJ 內部追蹤已讀取屬性用 |
| `Lab.LargeObject.Api.ContactInfo`（裝箱） | ~430MB | 巢狀 struct 同樣被 boxing |
| `Lab.LargeObject.Api.MemberAccount[]`（LOH 陣列本身） | ~33MB | 這次排查的主角，反而是最小的一塊 |

> 這些數字（除了 LOH 那行）是「Small」等級的抽樣配置量，每累積約 100KB 抽樣一次，代表**相對比例**、不是精確總量，但量級差距（GB vs MB）已經足以說明問題。

這些都是 gen0 等級的小物件配置，快、便宜、GC 很快清掉，不會造成 LOH 壓力——這也是為什麼整個排查過程中完全沒注意到它們。但如果在意的是**整體配置量／輸送量**而不只是 LOH，這才是真正的大頭，而且完全是意料之外的副作用：因為 `MemberAccount`／`ContactInfo` 是用 `JsonSerializer.Deserialize<T>(ref reader, options)` 走**反射式**反序列化，不是 Source Generator。

## 8. 反序列化配置量的最終解法：手刻 Utf8JsonReader 解析

第 7 節的建議（改用 Source Generator）**實測後被推翻**。過程跟結果都記錄在 `.issues/deserialize-allocation-reduction.issues.md`，這裡只記結論。

### 8.1 失敗的方向：Source Generator

換上 `JsonSerializerContext`（`[JsonSerializable]`）之後，`PooledMemberAccountArrayJsonConverter.Read()` 完全沒改，仍舊對每個陣列元素呼叫一次 `JsonSerializer.Deserialize<MemberAccount>(ref reader, options)`，只是讓 STJ 內部改用 source-gen 產生的 metadata 而不是反射。500 次 POST 的 trace 結果：

| 型別 | 反射版本 | Source Generator 版本 |
|---|---|---|
| `ReadStackFrame[]` | 4,143,974,424 bytes | 4,746,789,784 bytes（**漲**） |
| `ContactInfo`（裝箱） | 429,694,424 | 739,183,576（**漲將近 1 倍**） |
| `System.Text.Json.ArgumentState` | 未出現 | **1,270,832,616（全新冒出來的大戶）** |
| 主要問題型別總和 | ≈ 8.45GB | ≈ **10.12GB（更差）** |

原因：`MemberAccount`/`ContactInfo` 的屬性都宣告 `required init`，STJ 不管反射還是 Source Generator，都得配置一個 `ArgumentState` 追蹤物件確認所有 required 屬性設定齊全——Source Generator 這條路徑的追蹤更嚴格、配置更多。**真正的問題不是「metadata 從哪來」，是「每個元素都各自呼叫一次頂層 `Deserialize<T>`」這個呼叫模式本身**。

### 8.2 真正有效的方向：完全繞開 JsonSerializer.Deserialize&lt;T&gt;

改成跟 `PooledDoubleArrayJsonConverter` 一致的手法：`PooledMemberAccountArrayJsonConverter` 直接用 `Utf8JsonReader` 逐欄位手刻解析 `MemberAccount`／`ContactInfo`，用一般的 C# 物件初始化語法建構 struct，完全不進入 STJ 的 metadata／argument-state 機制。

第一版直接用 `reader.GetString()` 讀屬性名稱、`switch` 比對字串，`String` 配置量反而從 2.2GB 飆到 5.76GB——因為每個屬性**名稱**也都被當成一個新字串配置（`memberId`、`account`、`contact` 這些 key 本身，不只是值）。改用 `reader.ValueTextEquals("memberId"u8)` 在 UTF8 位元組層級比對屬性名稱（不呼叫 `GetString()`）之後才真正達到效果：

| 型別 | 反射版本（原始） | 手刻 v1（`GetString()` 比對屬性名） | 手刻 v2（`ValueTextEquals`） |
|---|---|---|---|
| `ReadStackFrame[]` | 4,143,974,424 | 消失 | 消失 |
| `String` | 2,204,865,744 | 5,758,927,456（**反而更差**） | **2,480,899,440** |
| `MemberAccount`/`ContactInfo`（裝箱） | 924M + 430M | 消失 | 消失 |
| `ArgumentState` | 未出現 | 消失 | 消失 |
| `BitArray` | 751,449,360 | 消失 | 消失 |
| `Byte[]` | 840,265,456 | 78,237,136 | 70,389,096 |
| 500 次 POST 耗時 | 22s | 15s | **12s** |

主要問題型別總和：反射版 ≈ 8.45GB → 手刻 v2 ≈ 2.55GB，**降了約 70%**，且 `ReadStackFrame[]`／裝箱／`ArgumentState`／`BitArray` 這幾項完全歸零。剩下的 `String` 配置（2.48GB）是欄位值本身（`account`、`displayName`、`email` 等字串內容）必要的配置，不是額外開銷，跟原始反射版本的必要字串量級相近，是合理的下限。

`MemberAccount[]`（LOH 陣列本身）大小維持在 ~33MB，跟這次改動無關（在意料之中，因為改的是元素解析邏輯，不是陣列容器的租用策略）。

### 8.3 這次調查示範的方法論

- 兩次改動都先建置、跑 `dotnet test`（4 個整合測試全過）才敢說「功能正常」；但**功能正常不等於效能有改善**——第一次改動（Source Generator）功能全過、效能卻更差，只有靠 `dotnet-trace` 才抓得到。
- 手刻 v1 改完先跑測試就抓到一個真的 bug（`status` 是數字 token 時 `GetString()` 直接丟例外，`Post_Members_...` 測試從綠燈變紅燈）——這正是先寫測試再實作能攔下的那種問題,不是靠肉眼 review 能穩定抓到的。
- 手刻 v1「解決了 boxing／ArgumentState，卻意外讓 String 配置暴增」，再次證明**要靠量測驗證每一步的效果，不能假設「用了更底層的 API 就一定比較快」**。

## 9. 架構延伸：從 ArrayPool 到 IAsyncEnumerable 串流解析（0 LOH 解法）

ArrayPool 透過「空間換時間」解決了重複配置 LOH 垃圾與頻繁 Gen2 GC 的問題，但它帶來了一個折衷：**必須常駐約數十至上百 MB 的 Buffer**。

如果業務邏輯不需要隨機存取（例如批次匯入、過濾、統計），.NET 提供了另一種更極致的解法：**`System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<T>(request.Body)` 串流解析**。

### 9.1 三種架構的完整實測對照（20,000 筆 MemberAccount，3.8MB Payload，10 並行 × 50 請求）

| 評測指標 | 1. `List<T>`（未池化） | 2. `PooledArray<T>`（ArrayPool 池化） | 3. `IAsyncEnumerable<T>`（串流解析） |
|---|---|---|---|
| **端點** | `/api/members-list` | `/api/members` | `/api/members-stream` |
| **總耗時** | **5,610 ms** | **3,695 ms** | **3,382 ms（最快）** |
| **LOH 峰值** | **55.4 MB（震盪）** | **60.3 MB（暖機後持平）** | **0 bytes（完全零 LOH）** |
| **LOH 最終值** | **42.1 MB** | **60.3 MB** | **0 bytes** |
| **Gen2 GC 次數** | **11 次（持續介入）** | **12 次（僅暖機期，後續歸 0）** | **6 次（常規小回收）** |
| **Working Set** | **278 MB** | **306 MB** | **142 MB（減半）** |

### 9.2 為什麼 `IAsyncEnumerable<T>` 能夠達成 0 LOH？

- **管線化邊讀邊算**：底層直接從 HTTP Request Stream 小區塊讀取，逐一反序列化單一 struct（64 bytes），直接在 Gen0 進行處理與釋放。
- **無大陣列容器**：整個過程中記憶體從未存在過 20,000 筆的連續大陣列，因此完全不觸碰 85,000 bytes 的 LOH 門檻。
- **雙贏結果**：相較於 ArrayPool 需要預留池化空間，串流解析同時達成了 **最低記憶體（142MB）** 與 **最高處理速度（3,382ms）**。

## 10. 結論

**已知事實**（有直接證據）：

- 陣列容器只要 ≥ 85,000 bytes 就一定進 LOH，這條規則沒有例外。
- 用 `ArrayPool<T>` 池化陣列容器，配合 `struct` 元素型別，能讓 LOH 配置在「池子暖機」後趨於零成長。
- 反射式反序列化巢狀 struct 產生的 boxing／內部機制配置量，遠大於 LOH 陣列本身；手刻 `Utf8JsonReader` + `ValueTextEquals` 能大幅削減 70% 配置量。
- 改用 `IAsyncEnumerable<T>` 串流解析可徹底免除大陣列配置，達成全程 0 LOH、Working Set 減半且處理速度最快。

**已推翻的假設**：

- ~~LOH 階梯成長是因為 ThreadPool 持續注入新 thread~~ ——實測 thread 數量淨變化為 0，此假設不成立。
- ~~改用 Source Generator 能降低反射式反序列化的配置量~~ ——實測配置量不降反升（8.45GB → 10.12GB），根因是 `required` 屬性的 argument-state 追蹤機制，不是 metadata 來源。

**待驗證**：

- 沒有直接證據解釋「為什麼恰好是那 8 次請求 cache miss」（可能跟 Kestrel 連線建立/拆除的時序有關，但沒有進一步追查）。

## 11. 附錄：如何重現

### 11.1 專案內建的腳本（`scripts/`）

```bash
# 一鍵執行三種寫法全自動對照實驗
./scripts/benchmark-all.sh

# 4MB 數值陣列負載實驗
./scripts/experiment-4mb.sh http://localhost:5138 10 50 all

# 20,000 筆複雜型別 (MemberAccount) 負載實驗
./scripts/experiment-members.sh http://localhost:5138 10 50 all
```

### 11.2 naive vs pooled 對照（`/api/readings`，double 陣列版本）

排查最開始用來驗證「pooling 有沒有用」的基準測試，20 併發 × 400 requests：

| | naive（每次都 `new double[]`） | pooled（本專案 ArrayPool 寫法） |
|---|---|---|
| gen2 GC 次數 | +38 | +1 |
| Working Set | 78MB → 191MB，之後停在高點 | 189MB → 215MB，之後停在高點 |

### 9.3 dotnet-trace 分析器

`dotnet-trace collect -p <pid> --profile gc-verbose -o trace.nettrace --duration <hh:mm:ss>` 收集 trace 之後，需要一支小工具解析 `.nettrace`（本身沒有現成的事件明細輸出指令）：

```xml
<!-- traceanalyzer.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Diagnostics.Tracing.TraceEvent" Version="3.1.16" />
  </ItemGroup>
</Project>
```

```csharp
// Program.cs
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

using var source = new EventPipeEventSource(args[0]);
var clrParser = new ClrTraceEventParser(source);

clrParser.GCAllocationTick += data =>
{
    if (data.AllocationKind == GCAllocationKind.Large)
    {
        Console.WriteLine($"t={data.TimeStampRelativeMSec,10:F1}ms  {data.TypeName,-40} {data.AllocationAmount64:N0} bytes");
    }
};

source.Process();
```

`dotnet run -- <path-to-nettrace>` 執行即可看到每一筆 LOH 配置的精確時間與大小。這支分析器沒有納入 repo（只是排查用的一次性工具），需要的話可以照上面的內容重建。
