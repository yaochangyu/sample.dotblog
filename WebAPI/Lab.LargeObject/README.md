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
│   ├── Program.cs                              # minimal API，兩個端點：/api/readings、/api/members
│   ├── PooledArray.cs                          # 包住租用陣列的 IDisposable wrapper（泛型，兩個端點共用）
│   ├── PooledDoubleArrayJsonConverter.cs       # double[] 專用的 ArrayPool JsonConverter
│   ├── MemberAccount.cs                        # 會員帳號網域模型（巢狀 struct：MemberAccount + ContactInfo）
│   └── PooledMemberAccountArrayJsonConverter.cs # MemberAccount[] 專用的 ArrayPool JsonConverter
├── tests/Lab.LargeObject.Api.Tests/
│   ├── LargeArrayEndpointTests.cs              # /api/readings 的整合測試
│   └── MemberAccountEndpointTests.cs           # /api/members 的整合測試
└── scripts/
    ├── load-test.sh                            # 對端點發送大量並行的大陣列 request
    └── observe-counters.sh                     # 用 dotnet-counters 觀察 GC/LOH 計數器
```

## 核心做法：ArrayPool + 自訂 JsonConverter

- **`PooledDoubleArrayJsonConverter`**：註冊在 `ConfigureHttpJsonOptions` 裡，攔截 `double[]` 的反序列化過程。不用 `new double[]`，改用 `ArrayPool<double>.Shared.Rent()` 租一塊 buffer，不夠大時倍增租用、歸還舊的，把 JSON 陣列的每個元素塞進去。
- **`PooledArray<T>`**：因為 `ArrayPool` 租出來的陣列長度**不等於**實際資料長度（會被無條件捨入到 bucket 大小），這個 `readonly struct` 額外記錄 `Length`，並實作 `IDisposable`——`Dispose()` 才會真的呼叫 `ArrayPool.Return()` 把陣列還回池子。
- **端點寫法**：

  ```csharp
  app.MapPost("/api/readings", ([FromBody] PooledArray<double> readings) =>
  {
      using (readings) // 用完立刻歸還，離開這個範圍前不能讓 readings 外流
      {
          // ...處理資料...
      }
  });
  ```

  租用陣列的生命週期被鎖死在單一 request 的處理範圍內，這是安全使用 `ArrayPool` 的關鍵：**歸還之後，池子隨時可能把同一塊記憶體租給別的並行 request**，一旦有程式碼在 `Dispose()` 之後還持有並使用這個陣列的參考，就會產生難以重現的資料錯亂 bug。

### 陣列元素是巢狀強型別物件時怎麼辦：`MemberAccount` 範例

`double` 是原生數值型別，陣列本身就是一整塊連續資料。實務上更常見的情況是**陣列元素是自訂的巢狀物件**（例如「會員帳號」，本身還帶一個 `ContactInfo`）。這種情境下同一套 ArrayPool 模式依然適用，但有兩個關鍵設計決定：

1. **元素型別要用 `struct`，不能用 `class`。** 陣列如果裝的是 `class`，陣列本身只存參考（指標），每個物件實體還是各自獨立 `new` 出來、散落在 heap 上——`ArrayPool<T>` 頂多幫你少配置那些指標，真正占大小的物件本體完全沒被池化，等於白做。改成 `struct`（`MemberAccount`、`ContactInfo` 都是 `readonly struct`），每個元素的資料就直接內嵌在陣列的連續記憶體塊裡，池化陣列才真的池化到資料本身。
2. **只有「陣列容器」需要手動接管，巢狀欄位交給 System.Text.Json 原生遞迴處理。** `PooledMemberAccountArrayJsonConverter` 的 `Read()` 一樣手動控制 `ArrayPool<MemberAccount>` 的租用/倍增，但讀到每個陣列元素時，不會逐欄位手刻解析，而是直接呼叫 `JsonSerializer.Deserialize<MemberAccount>(ref reader, options)` 讓 STJ 用標準方式遞迴處理該元素（包含巢狀的 `ContactInfo`）。ArrayPool 要解決的只是「陣列本身」這一個大配置，不需要、也不應該去手動處理每個小物件內部的巢狀結構。

  ```csharp
  app.MapPost("/api/members", ([FromBody] PooledArray<MemberAccount> members) =>
  {
      using (members)
      {
          // members.Span[i].Contact.Email ...
      }
  });
  ```

> 兩個 converter 都是元素型別專屬的（`double`、`MemberAccount` 各一個）。要支援新的陣列型別，照 `PooledMemberAccountArrayJsonConverter` 的模式另外寫一個；直接綁 `[FromBody] T[]` 會繞過整個 pooling 機制，等於白做。

## 快速開始

需要 .NET 10 SDK。

```bash
# 建置
dotnet build

# 跑測試
dotnet test

# 跑單一測試
dotnet test --filter "FullyQualifiedName~Post_Readings_接收超過LOH門檻的大陣列"

# 啟動 API（預設 http://localhost:5138，見 launchSettings.json）
dotnet run --project src/Lab.LargeObject.Api
```

測試都用 `WebApplicationFactory<Program>` 送真正的 HTTP request，payload 大小刻意超過 85,000 bytes 的 LOH 門檻：

- `LargeArrayEndpointTests.cs`：131,072 個 `double`（序列化後約 1MB），驗證回傳的 count/sum/average 正確。
- `MemberAccountEndpointTests.cs`：20,000 筆巢狀 `MemberAccount`（含 `ContactInfo`），驗證回傳的狀態統計正確。

兩者都另外涵蓋空陣列的邊界情況。

## 重現「LOH 飆升」並觀察 GC 行為

`scripts/` 底下兩支腳本，一支施壓、一支觀察，建議開三個終端機：

```bash
# terminal 1：啟動 API
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Lab.LargeObject.Api

# terminal 2：觀察 GC/LOH 計數器（需先安裝 dotnet-counters）
dotnet tool install -g dotnet-counters
./scripts/observe-counters.sh Lab.LargeObject.Api 60
# 也可以用 pid：./scripts/observe-counters.sh <pid> 60

# terminal 3：施壓（產生 1MB payload，並行打 request）
./scripts/load-test.sh http://localhost:5080 /api/readings 20 500
```

`observe-counters.sh` 是 `dotnet-counters collect` 的包裝，輪詢 `System.Runtime` provider 底下這幾個計數器，寫成 CSV：

| 計數器 | 說明 |
|---|---|
| `dotnet.gc.last_collection.heap.size`（拆 gen0/1/2/loh/poh） | 最近一次 GC 後各世代大小 |
| `dotnet.gc.last_collection.heap.fragmentation.size` | 碎片化程度（LOH 預設不 compact，會持續累積） |
| `dotnet.gc.collections` | 各世代 GC 觸發頻率 |
| `dotnet.gc.heap.total_allocated` | 累計配置量 |
| `dotnet.process.memory.working_set` | OS 層級的實體記憶體佔用（RSS），跟 GC heap 是不同層次 |

只看 LOH 大小變化：

```bash
grep 'generation=loh' scripts/counters-*.csv | grep 'heap.size'
```

`load-test.sh` 第一次執行會產生並快取一份 131,072 個 `double` 的 JSON payload（`scripts/payload-1mb.json`，約 1MB），用 `curl` + `xargs -P` 並行送出。

## 實測數據：pooled 版本 vs. 不 pooling 的對照組

用同一套腳本，20 併發 × 400 requests，分別打「每次都 `new double[]`」的 naive 寫法與這個專案的 pooled 寫法，實際觀察到：

| | naive（不 pooling） | pooled（ArrayPool，本專案的寫法） |
|---|---|---|
| 壓測期間的 gen2 GC 次數 | +38 次 | +1 次 |
| LOH 大小變化 | 持續飆升到 ~30MB 才回落到 plateau | 一次跳到高點後持平 |
| Working Set | 明顯上升後**停在高點不降**（不會自動還給 OS） | 同樣停在高點不降 |

關鍵差異不是「pooled 完全不會漲」——併發本身就需要同時存在多個 buffer，LOH 一樣會被撐高。真正的差異在 **GC 被迫介入的次數**：naive 每個 request 都製造一個新的 LOH 垃圾，逼 GC 一直收；pooled 版本重複利用同一批 buffer，幾乎不需要 GC 出手。

也順帶驗證了一件事：Working Set 一旦漲上去，就算之後沒有新的請求進來，也不會自動降回去——這是 .NET GC 預設不主動把記憶體還給 OS 的行為，在 Grafana 這類監控面板上看記憶體用量時要留意這一點。
