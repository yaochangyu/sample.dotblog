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

## 實測數據：三種寫法完整對照

在完全相同測試環境下（**20,000 筆巢狀 `MemberAccount`、3.8MB JSON Body、10 並行、50 筆請求**），使用 `scripts/benchmark-all.sh` 實測：

| 評測指標 | 1. `List<T>`（未池化） | 2. `PooledArray<T>`（ArrayPool 池化） | 3. `IAsyncEnumerable<T>`（串流解析） |
|---|---|---|---|
| **端點路徑** | `/api/members-list` | `/api/members` | `/api/members-stream` |
| **總處理耗時** | **5,610 ms**（最慢） | **3,695 ms**（快 34%） | **3,382 ms**（**最快，快 40%**） |
| **LOH 記憶體峰值** | **55.4 MB** | **60.3 MB** | **0 bytes（完全零 LOH）** |
| **LOH 穩態最終值** | **42.1 MB（持續上下震盪）** | **60.3 MB（暖機後持平）** | **0 bytes（全程無大物件）** |
| **壓測期間 Gen2 GC** | **11 次（頻繁介入）** | **12 次（僅暖機期，後續歸 0）** | **6 次（常規小回收，無 LOH 壓力）** |
| **實體記憶體 Working Set** | **278 MB** | **306 MB** | **142 MB（減半，極低佔用）** |

### 常見疑問：為什麼 `PooledArray<T>` 的記憶體數字看起來比 `List<T>` 高？

- **`PooledArray` 採「空間換時間」策略**：租用的 Buffer 在 Request 結束後歸還至池子保留給下一個請求複用（不丟給 GC），因此監控上看到的 60MB LOH 是「常駐可用的 Buffer 固定資產」。
- **`List<T>` 看到的 42MB 是「剛好被 GC 掃過後的垃圾殘留快照」**：`List<T>` 每次請求都在產生新的 1~2MB 垃圾，引發了 11 次耗損 CPU 的 Gen2 Full GC，表面看似佔用較少，實則代價最高。
- **`IAsyncEnumerable<T>` 達到「效能與記憶體雙贏」**：無需維護大 Buffer，直接從串流逐筆解析，LOH 徹底歸零，Working Set 僅 142MB。

---

## 重現實驗腳本

專案內建完整實驗工具：

```bash
# 1. 一鍵全自動對比三種寫法（會自動啟動 API、收集 counters 並輸出報表）
./scripts/benchmark-all.sh

# 2. 執行 4MB (524,288 double) 負載實驗
./scripts/experiment-4mb.sh http://localhost:5138 10 50 all

# 3. 執行 20,000 筆複雜型別 (MemberAccount) 負載實驗
./scripts/experiment-members.sh http://localhost:5138 10 50 all
```
