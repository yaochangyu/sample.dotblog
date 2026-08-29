# Lab.LargeObject

ASP.NET Core 接收大型（~1MB）JSON 陣列時，如何避免每個 request 都往 Large Object Heap（LOH）丟一個新物件的範例專案。附上重現「LOH 飆升」與觀察 GC 行為的腳本。

## 背景：為什麼會有這個專案

.NET 的 GC 有個規則：只要單一物件（陣列、字串等）配置時 **≥ 85,000 bytes**，就會被放進 LOH，這是寫死在 CLR 裡的規則，沒有例外。

如果一個 ASP.NET Core 端點直接用 `[FromBody] double[] data` 接收一個 ~1MB 的 JSON 陣列，`System.Text.Json` 預設會用 `new double[]` 反序列化——這個陣列本身就超過門檻，每個 request 都會製造一個新的 LOH 物件。物件用完立刻變垃圾沒錯，但：

- LOH 只有 gen2 GC 才會回收，回收後的空間預設也不會馬上還給作業系統。
- 高併發下，這些短命的大物件會逼著 GC 頻繁介入，造成明顯的記憶體飆升與 GC 壓力。

這個專案示範**正確的處理方式**：用 `ArrayPool<T>` 租用/歸還陣列，取代讓框架每次都配置新陣列，並提供腳本讓你**實際跑出飆升的過程**、比較兩種寫法的差異。

## 專案結構

```
Lab.LargeObject/
├── Lab.LargeObject.slnx
├── src/Lab.LargeObject.Api/
│   ├── Program.cs                              # minimal API（包含 PooledArray, List, IAsyncEnumerable 三種寫法端點）
│   ├── PooledArray.cs                          # 包住租用陣列的 IDisposable wrapper（泛型，共用）
│   ├── PooledDoubleArrayJsonConverter.cs       # double[] 專用的 ArrayPool JsonConverter
│   ├── MemberAccount.cs                        # 會員帳號網域模型（巢狀 struct：MemberAccount + ContactInfo）
│   └── PooledMemberAccountArrayJsonConverter.cs # MemberAccount[] 專用的 ArrayPool JsonConverter
├── tests/Lab.LargeObject.Api.Tests/
│   ├── LargeArrayEndpointTests.cs              # /api/readings, /api/readings-list, /api/readings-stream 整合測試
│   └── MemberAccountEndpointTests.cs           # /api/members, /api/members-list, /api/members-stream 整合測試
└── scripts/
    ├── load-test.sh                            # 基礎壓測腳本
    ├── observe-counters.sh                     # 用 dotnet-counters 觀察 GC/LOH 計數器
    ├── experiment-4mb.sh                       # 4MB (524,288 double) 負載對照實驗腳本
    ├── experiment-members.sh                   # 複雜型別 (20,000 MemberAccount) 負載對照實驗腳本
    └── benchmark-all.sh                        # 三種寫法一鍵全自動壓測與指標比對腳本
```

## 核心做法：三種架構的比較

專案實作並對比了處理大型 JSON 陣列的三種寫法：

1. **`List<T>`（未池化對照組）**：
   - 使用 `[FromBody] List<T>` 接收資料。
   - `System.Text.Json` 逐筆反序列化並動態擴容，在跨過 85,000 bytes 門檻後，底層陣列會直接落在 LOH。在中途擴容過程中連續遺棄大量暫存陣列，製造嚴重的短期 LOH 垃圾與頻繁的 Gen2 GC。
2. **`PooledArray<T>`（ArrayPool 池化）**：
   - 透過自訂 `JsonConverter` 接管反序列化，改用 `ArrayPool<T>.Shared.Rent()` 租用連續 Buffer。
   - 透過 `readonly struct PooledArray<T> : IDisposable` 封裝，端點在 `using` 範圍結束時呼叫 `Dispose()` 歸還至 Pool。
   - 暖機後 Buffer 永久複用，後續請求完全零新增 LOH 配置、零 Gen2 GC 壓力。
3. **`IAsyncEnumerable<T>`（串流解析 Streaming）**：
   - 使用 `JsonSerializer.DeserializeAsyncEnumerable<T>(request.Body)` 邊接收 HTTP Request 串流邊反序列化與處理。
   - **完全不需要在記憶體中配置包含所有元素的大陣列**，單一元素直接在 Gen0 處理完即釋放，達成 **全程 0 LOH 配置** 且記憶體佔用極低。

---

## 實測數據：9 種全組合完整對照總表

在完全相同測試環境下（**10 並行、50 筆請求**），針對 **3 種資料型別 × 3 種反序列化架構（共 9 種組合）** 使用 `scripts/benchmark-all-9.sh` 實測對比：

| 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | Gen0 GC<br>頻率/量級 | Gen2 GC<br>介入程度 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |
|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---|
| **1. 原生數值**<br>*(double 4MB)* | **List (未池化)** | `/api/readings-list` | 4,030 | 中 (46M) | ⚠️ 高 (27M) | 72 MB | 198 MB | ❌ 擴容連續遺棄多個暫存陣列，製造 LOH 垃圾 |
| **1. 原生數值**<br>*(double 4MB)* | **ArrayPool (池化)** | `/api/readings` | 5,430 | 中 (57M) | ⚠️ 高 (29M) | 122 MB | 323 MB | ⚡ 陣列完整池化，暖機後重複複用 4MB Buffer |
| **1. 原生數值**<br>*(double 4MB)* | **Streaming (串流)** | `/api/readings-stream` | **3,963** | 低 (21M) | ✅ 低 (17M) | **0 MB** | **115 MB** | 🏆 **最快且記憶體最低**，全程零大物件 |
| **2. 巢狀結構**<br>*(Struct 20k 筆)* | **List (未池化)** | `/api/members-list` | 3,720 | 中 (59M) | ⚠️ 極高 (101M) | 33 MB | 235 MB | ❌ 大量短命 Struct 陣列垃圾，逼出大量 Gen2 GC |
| **2. 巢狀結構**<br>*(Struct 20k 筆)* | **ArrayPool (池化)** | `/api/members` | 3,769 | **極低 (0.6M)** | ⚠️ 暖機高 (130M) | 62 MB | 275 MB | ⚡ **Gen0 降幅達 99%**，資料內嵌於 Buffer 完整池化 |
| **2. 巢狀結構**<br>*(Struct 20k 筆)* | **Streaming (串流)** | `/api/members-stream` | **3,575** | 低 (1.5M) | ✅ 低 (24M) | **0 MB** | **131 MB** | 🏆 **Struct 最佳解**，0 LOH、實體記憶體減半 |
| **3. 參考型別**<br>*(Class 20k 筆)* | **List (未池化)** | `/api/members-class-list` | **3,171** | 極高 (95M) | ❌ 劇烈 (365M) | 3 MB | 249 MB | ⚠️ LOH 雖小，但 4 萬個物件實體散落 Gen0 狂觸發 GC |
| **3. 參考型別**<br>*(Class 20k 筆)* | **ArrayPool (池化)** | `/api/members-class-pooled` | 3,384 | 高 (16M) | ❌ 劇烈 (290M) | 6 MB | 351 MB | ⚠️ **池化效益極低**，只池化到指標，無法消除實體 GC |
| **3. 參考型別**<br>*(Class 20k 筆)* | **Streaming (串流)** | `/api/members-class-stream` | 4,406 | 低 (4.6M) | ✅ 低 (26M) | **0 MB** | **139 MB** | 🏆 **Class 最佳解**，逐筆讀取釋放，記憶體維持極低 |

---

## 核心剖析：為什麼 ArrayPool「Working Set（記憶體佔用）較大，但 Gen2 GC 卻極少」？

在監控指標上，常會看到 `PooledArray` 的 Working Set（實體記憶體）高達 300MB+，看似比 `List<T>` 還佔記憶體，但 Gen2 GC 卻直接歸零。這背後是 .NET 記憶體管理的核心運作機制：

### 1. 為什麼 Gen2 GC 幾乎為 0？（Zero Allocation 效應）
- **沒有垃圾，GC 就不需要出動**：
  - 在 `List<T>` 寫法中，每次 Request 建立的大陣列在用完後失去引用變成「垃圾」，迫使 GC 必須進行代價高昂的 Gen2 Full GC 來清理 LOH。
  - 在 `PooledArray<T>` 寫法中，Buffer 來自 `ArrayPool`，用完立刻透過 `Dispose()` 歸還給 `ArrayPool`。**這塊記憶體從未變成垃圾**，LOH 上沒有垃圾堆積，因此 GC 完全沒有介入回收的理由，Gen2 GC 觸發次數自然降為 0。

### 2. 為什麼 Working Set（實體記憶體）會維持在較大數值？
- **ArrayPool 的常駐預留（空間換時間）**：
  - `ArrayPool` 在高並發（如 10 並行）下，會租用並建立多個對應 Bucket 大小的連續 Buffer（例如 10 塊 2MB~4MB 的陣列）。
  - 當請求結束歸還時，Buffer 是被**保留在池子（Process 內部記憶體）中**隨時等待下一個請求複用，並不會釋放回作業系統。
- **.NET GC 預設不主動向 OS 歸還記憶體（Decommit）**：
  - .NET GC 在向作業系統申請實體記憶體（Working Set）後，為了維持高效能，預設不會在沒有外部記憶體壓力的情況下主動將 Committed 記憶體還給作業系統。
  - 因此 Working Set 會維持在「並行數 $\times$ Buffer 大小」的穩態高點，這是專為**換取零 GC 停頓與高吞吐量**而預先持有的固定資產。

---

## 重現實驗腳本

專案內建完整實驗工具：

```bash
# 1. 一鍵全自動對比 Struct vs Class 完整 6 種組合
./scripts/benchmark-class-vs-struct.sh

# 2. 一鍵全自動對比三種架構（List vs ArrayPool vs Streaming）
./scripts/benchmark-all.sh

# 3. 執行 4MB (524,288 double) 負載實驗
./scripts/experiment-4mb.sh http://localhost:5138 10 50 all

# 4. 執行 20,000 筆複雜型別 (MemberAccount) 負載實驗
./scripts/experiment-members.sh http://localhost:5138 10 50 all
```
