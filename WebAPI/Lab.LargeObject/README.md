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
│   ├── Program.cs                                  # minimal API（包含 4 種型別 × 3 種架構共 12 個端點）
│   ├── PooledArray.cs                              # 包住租用陣列的 IDisposable wrapper（泛型，共用）
│   ├── PooledDoubleArrayJsonConverter.cs           # double[] 專用的 ArrayPool JsonConverter
│   ├── PooledStringArrayJsonConverter.cs           # string[] 專用的 ArrayPool JsonConverter
│   ├── MemberAccount.cs                            # 會員帳號值型別模型（巢狀 struct：MemberAccount + ContactInfo）
│   ├── PooledMemberAccountArrayJsonConverter.cs     # MemberAccount[] 專用的 ArrayPool JsonConverter
│   ├── MemberAccountClass.cs                       # 會員帳號參考型別模型（巢狀 class：MemberAccountClass + ContactInfoClass）
│   └── PooledMemberAccountClassArrayJsonConverter.cs # MemberAccountClass[] 專用的 ArrayPool JsonConverter
├── tests/Lab.LargeObject.Api.Tests/
│   ├── LargeArrayEndpointTests.cs                  # /api/readings* 整合測試
│   ├── StringEndpointTests.cs                      # /api/strings* 整合測試
│   ├── MemberAccountEndpointTests.cs               # /api/members* (struct) 整合測試
│   └── MemberAccountClassEndpointTests.cs          # /api/members-class* (class) 整合測試
└── scripts/
    ├── benchmark-all-12.sh                         # 12 種全組合一鍵全自動壓測與持久化報表腳本
    ├── benchmark-class-vs-struct.sh                # 6 種 Struct vs Class 對照實驗腳本
    └── benchmark-all.sh                            # 3 種架構對照實驗腳本
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

### 實測數據：12 種全組合完整對照大一統總表

在完全相同測試環境下（**10 並行、50 筆請求**），針對 **4 種資料型別 × 3 種反序列化架構（共 12 種組合）**，透過 .NET 10 原生 `GC.GetTotalPauseDuration()` 與 `dotnet-counters` 實測：

| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |
|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 🏆 **S 級** | **1. 原生數值**<br>*(double 4MB)* | **Streaming (串流)** | `/api/readings-stream` | **5,121** | **12.6 ms (0.42%)** | **4 次** | **4 次** | ✅ **4 次 (常規)** | **0 MB** | **114 MB** | 🏆 **最快、停頓最短 (12ms)、記憶體最低** |
| ⚡ **A 級** | **1. 原生數值**<br>*(double 4MB)* | **ArrayPool (池化)** | `/api/readings` | 3,850 | 46.5 ms (1.01%) | 13 次 | 13 次 | ⚠️ 13 次 (常規) | 110 MB | 325 MB | ⚡ 陣列完整池化，暖機後重複複用 4MB Buffer |
| ❌ **D 級** | **1. 原生數值**<br>*(double 4MB)* | **List (未池化)** | `/api/readings-list` | 3,230 | 45.7 ms (0.86%) | 20 次 | 20 次 | ⚠️ 20 次 (常規) | 81 MB | 192 MB | ❌ 擴容連續拋棄暫存陣列，製造 LOH 垃圾 |
| 🏆 **S 級** | **2. 原生字串**<br>*(string 50k 筆)* | **Streaming (串流)** | `/api/strings-stream` | **2,156** | **42.5 ms (0.93%)** | 37 次 | **7 次** | ✅ **7 次 (常規)** | **0 MB** | **132 MB** | 🏆 **String 最佳解**，0 LOH、停頓極短、記憶體極低 |
| ⚠️ **C 級** | **2. 原生字串**<br>*(string 50k 筆)* | **ArrayPool (池化)** | `/api/strings` | 2,279 | 91.0 ms (1.99%) | 24 次 | 11 次 | ❌ **10 次 (劇烈)** | 13 MB | 344 MB | ⚠️ **池化效益低**，僅池化指標陣列，字串實體仍散落 Gen0 |
| ❌ **D 級** | **2. 原生字串**<br>*(string 50k 筆)* | **List (未池化)** | `/api/strings-list` | 2,255 | **132.1 ms (2.80%)** | 26 次 | 15 次 | ❌ **11 次 (劇烈)** | 10 MB | 303 MB | ❌ 擴容指標陣列衝破 85KB LOH，且引發高頻 GC |
| 🏆 **S 級** | **3. 巢狀結構**<br>*(Struct 20k 筆)* | **Streaming (串流)** | `/api/members-stream` | **3,078** | **56.7 ms (1.04%)** | 34 次 | **6 次** | ✅ **6 次 (常規)** | **0 MB** | **137 MB** | 🏆 **Struct 最佳解**，停頓降 76%、0 LOH、記憶體減半 |
| ⚡ **A 級** | **3. 巢狀結構**<br>*(Struct 20k 筆)* | **ArrayPool (池化)** | `/api/members` | 2,903 | 86.2 ms (1.58%) | 20 次 | 13 次 | ⚠️ 11 次 (常規) | 62 MB | 274 MB | ⚡ **需隨機存取首選**，資料內嵌於 Buffer 完整池化 |
| ❌ **D 級** | **3. 巢狀結構**<br>*(Struct 20k 筆)* | **List (未池化)** | `/api/members-list` | 3,002 | **232.0 ms (4.25%)** | 28 次 | 21 次 | ❌ **16 次 (劇烈)** | 28 MB | 280 MB | ❌ **GC 停頓極長 (232ms)**，短命陣列引發頻繁 Full GC |
| 🛡️ **B 級** | **4. 參考型別**<br>*(Class 20k 筆)* | **Streaming (串流)** | `/api/members-class-stream` | 3,406 | **55.7 ms (0.99%)** | 39 次 | **5 次** | ✅ **5 次 (常規)** | **0 MB** | **127 MB** | 🏆 **Class 最佳解**，GC 停頓降 69%，記憶體維持極低 |
| ⚠️ **C 級** | **4. 參考型別**<br>*(Class 20k 筆)* | **ArrayPool (池化)** | `/api/members-class-pooled` | 3,199 | 106.3 ms (1.97%) | 20 次 | 10 次 | ❌ **9 次 (劇烈)** | 6 MB | 297 MB | ⚠️ **池化效益低**，僅省下指標，物件依舊觸發長時間 GC |
| ❌ **D 級** | **4. 參考型別**<br>*(Class 20k 筆)* | **List (未池化)** | `/api/members-class-list` | 2,853 | 180.6 ms (3.41%) | 23 次 | 12 次 | ❌ **8 次 (劇烈)** | 5 MB | 226 MB | ⚠️ 4 萬個 Class 實體散落 Gen0，GC 停頓高達 180ms |

---

## 核心剖析：深入 .NET GC 與 LOH 的運作本質

### 1. Gen2 GC 的回收週期是什麼？（Budget-driven 非定時器）

在 .NET CLR 中，**Gen2 GC 沒有固定時間週期（不是每隔幾秒執行一次）**，而是採用**「事件驅動」與「動態預算（Budget-driven）」**機制：
- **世代晉升累積**：Gen0 滿載觸發 Gen0 GC $\rightarrow$ 存活物件晉升 Gen1 $\rightarrow$ Gen1 滿載晉升 Gen2 $\rightarrow$ 累積超過 **Gen2 Budget** 時觸發 Full GC。
- **LOH 配置門檻跨越**：當 $\ge 85\text{KB}$ 的大物件配置量打爆 LOH 動態門檻時，強制觸發 Gen2 GC（這就是 `List<T>` 頻繁觸發 GC 的主因）。

> **常見疑問：Streaming 模式 LOH 為 0 MB，這 4~7 次 Gen2 GC 是哪裡產生的？**
> - **答案：完全沒有任何地方產生 LOH 大物件（LOH 配置量確鑿為 0 bytes）。**
> - 在 50 筆 4MB 請求中，共處理了 **2,621 萬筆 `double`、250 萬筆 `string` 或 100 萬筆 `MemberAccount`**。
> - 雖然沒有大物件，但在處理數百萬筆資料的非同步網路串流時，產生了大量 Gen0 微型物件（如 Kestrel Socket Buffer、`await foreach` 狀態機、`IAsyncEnumerator` 物件）。
> - 剛好處於非同步 I/O 等待中的微型物件會自然晉升至 Gen1，進而晉升至 Gen2 並填滿了 Gen2 的動態 Budget 配額，因而觸發了常規 Gen2 GC。
> - **核心證據（GC 停頓時間）**：因為 LOH 為 0 且沒有大垃圾，每次回收只要 0.5ms，50 筆請求的累積 GC 停頓**僅 12.6~56.7 ms（佔總時間 0.4%~1.0%）**；反觀 `List<T>` 因 LOH 垃圾觸發的 Gen2 停頓高達 **132~232 ms**。

### 2. 為什麼未池化的 LOH 大物件會導致 OOM？

未池化的大物件（如 `List<T>`）導致系統 OOM 的 3 大真實途徑：
1. **LOH 記憶體碎片化（Fragmentation）**：LOH 預設不壓縮（No Compaction），GC 回收後只留下 Free List 洞。若找不到足夠大的連續空間，即使剩餘總記憶體充足也會拋出 `OutOfMemoryException`。
2. **K8s 容器記憶體硬上限撞爆（OOMKilled）**：GC 標記回收 $\neq$ 把記憶體還給作業系統（No Decommit）。高並發下多個請求連續擴容將 Working Set 推向 400MB~500MB，直接撞上 Pod `limits.memory` 被 Linux Kernel 砍死（Exit Code 137）。
3. **GC 追趕不上配置速度（GC Thrashing 惡性循環）**：頻繁 Gen2 GC 搶佔 30%~50% CPU $\rightarrow$ API 處理變慢 $\rightarrow$ 請求在記憶體中排隊積壓 $\rightarrow$ 記憶體被積壓請求灌滿 $\rightarrow$ OOM。

### 3. 為什麼 ArrayPool「Working Set 較大，但 Gen2 GC 卻極少」？

- **Zero Allocation 效應**：`PooledArray<T>` 的 Buffer 用完即歸還至 ArrayPool，**這塊記憶體從未變成垃圾**，LOH 上沒有垃圾堆積，GC 自然無需介入清理。
- **空間換時間的固定資產**：ArrayPool 在高並發下租用多個連續 Buffer，歸還後留在 Process 記憶體中供後續請求重複使用，且 .NET GC 預設不向 OS decommit，因此 Working Set 會維持在「並行數 $\times$ Buffer 大小」的平穩高點。

---

## 寫法優劣排序與技術選型 SOP

### 1. 全架構優劣梯隊

| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |
|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 🥇 **S 級** | **Struct / double / string** | **Streaming (串流)** | `/api/*-stream` | **2.1~5.1s** | **12.6~56.7 ms (極短)** | **4~37 次** | **4~7 次** | ✅ **4~7 次 (常規)** | **0 MB** | **114~137 MB (減半)** | 🏆 **效能與記憶體雙冠王**，全程 0 大物件、零暖機成本 |
| 🥈 **A 級** | **Struct / double** | **ArrayPool (池化)** | `/api/readings`, `/api/members` | 2.9~3.8s | 46.5~86.2 ms | 13~20 次 | 13 次 | ⚠️ **11~13 次 (常規)** | 62~110 MB | 274~325 MB (常駐) | ⚡ **需隨機存取首選**，資料內嵌於 Buffer 完整池化 |
| 🥉 **B 級** | **Class (參考型別)** | **Streaming (串流)** | `/api/members-class-stream` | 3.4s | **55.7 ms (極短)** | 39 次 | **5 次** | ✅ **5 次 (常規)** | **0 MB** | **127 MB (減半)** | 🛡️ **既有 Class 模型無法改為 struct 時的最佳解** |
| ⚠️ **C 級** | **Class / string** | **ArrayPool (池化)** | `/api/strings`, `/api/members-class-pooled` | 2.2~3.1s | 91.0~106.3 ms (偏長) | 20~24 次 | 10~11 次 | ❌ **9~10 次 (劇烈)** | 6~13 MB | 297~344 MB (偏高) | ❌ **白做工**，只池化到指標，物件/字串實體依舊在 Gen0 瘋狂產出垃圾 |
| 🚫 **D 級** | **所有型別 (未池化)** | **List (未池化)** | `/api/*-list` | 2.2~3.2s | **45.7~232.0 ms (極長)** | 20~28 次 | 12~21 次 | ❌ **8~20 次 (劇烈)** | 5~81 MB | 192~303 MB | 💥 **效能毒藥**，擴容放大效應引發頻繁 Gen2 GC 與 OOM 風險 |

### 2. 生產環境選型決策樹

```text
接收大型 JSON 陣列 (≥ 85KB)
├── 步驟 1：業務邏輯需要拿到完整陣列才能運算嗎？
│   │
│   ├── 【否（可逐筆累加/過濾/存入 DB）】
│   │   👉 唯一首選：IAsyncEnumerable<T> 串流解析 (S 級)
│   │
│   └── 【是（必須做隨機索引、排序、矩陣運算）】
│       │
│       └── 步驟 2：資料模型可以設計成 struct 嗎？
│           │
│           ├── 【能】👉 選擇：readonly struct + ArrayPool + 自訂 JsonConverter (A 級)
│           │
│           └── 【不能（既有 class）】👉 強制重構業務邏輯為串流處理，切勿直接用 List<T>
```

---

## Response（回傳大型資料）實測數據與架構對照

回傳大型資料（例如 20,000 筆資料、數 MB JSON）時，**寫法不當同樣會引發 LOH 飆升與 OOM 風險**。

在完全相同測試環境下（**10 並行、50 筆請求**），針對 4 種 Response 架構進行實測：

| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |
|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 🏆 **S 級** | **Response 回傳 (20k 筆)** | **Streaming (串流回傳)** | `/api/export-stream` | **782** | **52.7 ms (1.59%)** | **41 次** | **3 次** | ✅ **2 次 (極低)** | **0 MB** | **102 MB** | 🏆 **最快(782ms)、0 LOH、停頓短、記憶體最低** |
| ⚡ **A 級** | **Response 回傳 (20k 筆)** | **ArrayPool (池化回傳)** | `/api/export-pooled` | 1,165 | 196.2 ms (5.35%) | 26 次 | 15 次 | ⚠️ 10 次 (常規) | 126 MB | 443 MB | ⚡ 租用 Buffer 序列化後歸還，避免多次分散配置 |
| ❌ **D 級** | **Response 回傳 (20k 筆)** | **List (未池化回傳)** | `/api/export-list` | 1,251 | 115.4 ms (3.08%) | 19 次 | 9 次 | ❌ 7 次 (劇烈) | 18 MB | 217 MB | ❌ 每次請求建立大 List 佔據 LOH，引發 GC 停頓 |
| 💥 **D 級** | **Response 回傳 (20k 筆)** | **SerializeToUtf8Bytes (byte[])** | `/api/export-bytes` | 1,195 | **165.7 ms (4.64%)** | 24 次 | 18 次 | ❌ **16 次 (劇烈)** | **194 MB** | **520 MB** | 💥 **LOH 與 Working Set 暴衝 5 倍(520MB)，OOM 風險最高** |

### 核心發現：
1. **`SerializeToUtf8Bytes` 是最大的記憶體地雷**：直接將 20k 筆資料轉成單一 `byte[]`（~3.7MB），每次 Request 都向 LOH 丟入 3.7MB 垃圾，LOH 峰值直接飆至 **194 MB**，Working Set 飆上 **520 MB**。
2. **`IAsyncEnumerable<T>` 是 Response 最佳解**：直接以 HTTP 串流逐筆寫出，**LOH 為 0 MB**，耗時僅 **782 ms**，Working Set 僅 **102 MB**（省下 80% 實體記憶體）。

---

## 重現實驗腳本

專案內建完整實驗工具（支援結果持久化與重複渲染）：

```bash
# 1. 執行 12 種全組合 Request 壓測並將結果持久化至 scripts/latest-results.json
./scripts/benchmark-all-12.sh

# 2. ⚡ 秒級重用上次 Request 測試結果，直接輸出 Markdown 大一統總表（無需重跑）
./scripts/benchmark-all-12.sh --report

# 3. 執行 4 種 Response 壓測並將結果持久化至 scripts/latest-response-results.json
./scripts/benchmark-response.sh

# 4. ⚡ 秒級重用上次 Response 測試結果，直接輸出 Markdown 表格（無需重跑）
./scripts/benchmark-response.sh --report

# 5. 6 種 Struct vs Class 對照實驗
./scripts/benchmark-class-vs-struct.sh

# 6. 3 種架構 (List vs ArrayPool vs Streaming) 對照實驗
./scripts/benchmark-all.sh
```
