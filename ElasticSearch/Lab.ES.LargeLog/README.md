# Elasticsearch 8.x 與 .NET 10 Web API 實戰教學指南

本教學為完整的 Elasticsearch (ES) 實戰手冊，涵蓋 **SQL 與 ES 概念差異圖解**、**Docker 測試環境建立**、**手把手實操與終端機真實執行畫面**、**每日一億筆海量時序資料（Data Stream + ILM）架構設計**、**每日 Log Doc 是否需額外指定日期關鍵觀念**，以及 **ASP.NET Core 10 Web API 的完整 CRUD 實作與自動化測試**。

---

## 目錄
1. [核心概念：SQL vs Elasticsearch 圖解](#1-核心概念sql-vs-elasticsearch-圖解)
2. [環境準備：Docker Compose 快速啟動](#2-環境準備docker-compose-快速啟動)
3. [快速上手：手把手實操與終端機真實執行畫面](#3-快速上手手把手實操與終端機真實執行畫面)
4. [架構設計：每日一億筆時序資料（Data Stream + ILM）](#4-架構設計每日一億筆時序資料data-stream--ilm)
   - [4.1 關鍵觀念：Index 根據欄位建立後，每日 Log Doc 還要額外指定？](#41-關鍵觀念index-根據欄位建立後每日-log-doc-還要額外指定)
   - [4.2 初始化 ILM 與 Data Stream 腳本](#42-初始化-ilm-與-data-stream-腳本)
5. [ASP.NET Core 10 Web API 實作](#5-aspnet-core-10-web-api-實作)
6. [端對端驗證與自動化測試](#6-端對端驗證與自動化測試)
7. [維運避坑指南與最佳實踐](#7-維運避坑指南與最佳實踐)

---

## 1. 核心概念：SQL vs Elasticsearch 圖解

### 1.1 名詞概念與適用時機
| 關聯式資料庫 (SQL) | Elasticsearch (ES) | 說明 |
|---|---|---|
| **Database** | *(Cluster 管理)* | ES 叢集內管理多個 Index |
| **Table** | **Index** (索引) | 存放相同邏輯結構的資料集合 |
| **Schema** | **Mapping** (映射) | 定義欄位名稱與型態（`text`, `keyword`, `date` 等） |
| **Row** | **Document** (文件) | 一筆獨立的資料，以 **JSON** 格式儲存 |
| **Column** | **Field** (欄位) | JSON 物件中的 Key-Value 鍵值對 |
| **Primary Key** | **`_id`** | 每筆 Document 在 Index 內的唯一識別碼 |
| **SQL Query** | **Query DSL** | 基於 JSON 的查詢語法或 RESTful API |

**選型建議**：
- **用 SQL**：需強 ACID 交易保證（如轉帳）、頻繁跨表 JOIN 與嚴格外鍵約束。
- **用 ES**：需極快模糊/全文檢索、海量時序日誌（Logs/Metrics）的高速寫入與即時聚合分析。

### 1.2 底層檢索差異：B+ Tree vs 倒排索引 (Inverted Index)
- **SQL (正向儲存)**：以「列」為核心，搜尋 `LIKE '%關鍵字%'` 需逐筆全表掃描，資料量大時極慢。
- **ES (倒排索引)**：寫入時先將文字分詞（Tokenize），建立「單字指向文件 ID」的字典。搜尋時直接查字典，毫秒級返回命中結果，無需掃描全表。

---

## 2. 環境準備：Docker Compose 快速啟動

在專案目錄下啟動單節點測試環境：
```yaml
# docker-compose.yml
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.17.0
    container_name: es-lab
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false # 本機測試關閉驗證，正式環境請務必開啟
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    ports:
      - "9200:9200"
    healthcheck:
      test: ["CMD-SHELL", "curl -s http://localhost:9200/_cluster/health | grep -q '\"status\":\"green\"\\|\"status\":\"yellow\"'"]
      interval: 5s
      timeout: 5s
      retries: 20
```
啟動指令：`docker compose up -d`

---

## 3. 快速上手：手把手實操與終端機真實執行畫面

### 步驟 1：建立 Index（相當於 `CREATE TABLE`）
```bash
curl -X PUT "http://localhost:9200/my-first-index" \
  -H "Content-Type: application/json" \
  -d '{
    "settings": {
      "number_of_shards": 1,
      "number_of_replicas": 0
    },
    "mappings": {
      "properties": {
        "title": { "type": "text" },
        "category": { "type": "keyword" },
        "price": { "type": "double" },
        "createdAt": { "type": "date" }
      }
    }
  }'
```

**🖥️ 終端機回應畫面：**
```json
{
  "acknowledged": true,
  "shards_acknowledged": true,
  "index": "my-first-index"
}
```

---

### 步驟 2：建立 Document（相當於 `INSERT INTO`）

#### 方式 A：指定 Document ID 寫入（`PUT /<index>/_doc/<id>`）
```bash
curl -X PUT "http://localhost:9200/my-first-index/_doc/doc-001" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Elasticsearch 實戰指南",
    "category": "Book",
    "price": 580.0,
    "createdAt": "2026-08-23T16:50:00Z"
  }'
```

**🖥️ 終端機回應畫面：**
```json
{
  "_index": "my-first-index",
  "_id": "doc-001",
  "_version": 1,
  "result": "created",
  "_shards": {
    "total": 1,
    "successful": 1,
    "failed": 0
  },
  "_seq_no": 0,
  "_primary_term": 1
}
```

#### 方式 B：自動產生 Document ID 寫入（`POST /<index>/_doc`，高吞吐量推薦）
```bash
curl -X POST "http://localhost:9200/my-first-index/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "ASP.NET Core 10 開發筆記",
    "category": "Book",
    "price": 620.0,
    "createdAt": "2026-08-23T16:50:00Z"
  }'
```

**🖥️ 終端機回應畫面：**
```json
{
  "_index": "my-first-index",
  "_id": "LK7PLaABbPQ5FnXdYYdS",
  "_version": 1,
  "result": "created",
  "_shards": {
    "total": 1,
    "successful": 1,
    "failed": 0
  },
  "_seq_no": 1,
  "_primary_term": 1
}
```

---

### 步驟 3：查詢 Document（相當於 `SELECT`）

#### 方式 A：依 ID 查詢單筆
```bash
curl "http://localhost:9200/my-first-index/_doc/doc-001"
```

**🖥️ 終端機回應畫面：**
```json
{
  "_index": "my-first-index",
  "_id": "doc-001",
  "_version": 1,
  "_seq_no": 0,
  "_primary_term": 1,
  "found": true,
  "_source": {
    "title": "Elasticsearch 實戰指南",
    "category": "Book",
    "price": 580.0,
    "createdAt": "2026-08-23T16:50:00Z"
  }
}
```

#### 方式 B：全文檢索搜尋
```bash
curl -X POST "http://localhost:9200/my-first-index/_search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "match": {
        "title": "Elasticsearch"
      }
    }
  }'
```

**🖥️ 終端機回應畫面：**
```json
{
  "took": 2,
  "timed_out": false,
  "_shards": {
    "total": 1,
    "successful": 1,
    "skipped": 0,
    "failed": 0
  },
  "hits": {
    "total": {
      "value": 1,
      "relation": "eq"
    },
    "max_score": 0.8754687,
    "hits": [
      {
        "_index": "my-first-index",
        "_id": "doc-001",
        "_score": 0.8754687,
        "_source": {
          "title": "Elasticsearch 實戰指南",
          "category": "Book",
          "price": 580.0,
          "createdAt": "2026-08-23T16:50:00Z"
        }
      }
    ]
  }
}
```

---

## 4. 架構設計：每日一億筆時序資料（Data Stream + ILM）

面對每天 1 億筆（平均每秒千筆、尖峰 10,000+ docs/sec）的高吞吐時序資料，核心原則如下：

1. **永遠自動產生 Document ID**：自訂 ID 會觸發 ES 內部 Version Check 產生隨機磁碟讀取；自動產生 ID 為純 Append 寫入，效能提升 30%~50%。
2. **採用 Data Stream + ILM 架構**：避免手動維護每日索引名稱與過期刪除。

---

### 4.1 關鍵觀念：Index 根據欄位建立後，每日 Log Doc 還要額外指定？（前因後果與深度解析）

在時序日誌（Time Series Logs）系統的演進中，開發者常有疑問：「**既然 Index 已經定義好了欄位 Mapping，那每天寫入 Log 時，應用程式到底要不要在程式碼中指定當天日期？**」

#### 1. 前因：傳統手動按日建索引（Time-based Index）的痛點
早期（ES 7 以前）常見的做法是由應用程式在發送請求時，動態組裝當天的日期作為索引名稱（例如 `logs-2026.08.23`）：
* **程式碼維護複雜**：應用程式端必須在每次寫入時計算 `$"logs-{DateTime.UtcNow:yyyy.MM.dd}"`，若遇到時區轉換、跨日臨界點、延遲到達的 Log，容易發生寫錯索引或產生分散碎索引的問題。
* **分片大小極度不均勻（Over-sharding）**：
  * 業務離峰日（如週末或連假）Log 量少，但依然會建立出完整的 Shard，造成大量 < 1GB 的小分片，白白耗盡 Elasticsearch Node 的 JVM Heap 記憶體。
  * 業務尖峰日（如促銷活動）Log 量暴增，單日索引可能膨脹至數百 GB，導致單一 Shard 過大、查詢與復原變慢。
* **過期清理繁瑣**：必須在外部另外撰寫 CronJob 腳本（或使用 Curator 工具），每天定時掃描並下指令刪除 30 天前的舊索引名稱。

---

#### 2. 後果與解法：現代 Data Stream + ILM 架構
Elasticsearch 官方推出 **Data Stream** 就是為了解決上述痛點：
* **寫入端點永遠固定**：應用程式的寫入目標永遠是同一個抽象名稱（如 `POST /logs-app-prod/_bulk`），**程式碼中完全不需要任何計算日期或字串組裝邏輯**。
* **分片容量自動最佳化**：由 ILM（Index Lifecycle Management）在底層全自動監控。當底層當前索引累積滿 **40 GB**（或滿 1 天）時，ES 會自動執行 **Rollover** 產生下一個 Backing Index（例如 `.ds-logs-app-prod-2026.08.23-000001`），確保每個 Shard 都維持在最佳效能尺寸（10~50GB）。
* **生命週期自動清理**：ILM 內建自動過期機制，滿 30 天的資料會自動在底層被安全刪除，維運完全零負擔。

---

#### 3. 傳統做法 vs 現代 Data Stream 做法完整對照表

| 比較項目 | 傳統做法（手動按日建索引） | 現代推薦做法（Data Stream + ILM） |
|---|---|---|
| **寫入目標端點** | 每天動態變化：<br>`POST /logs-2026.08.23/_doc` | **永遠固定不變**：<br>`POST /logs-app-prod/_bulk` |
| **程式端是否需算日期** | **要**（需寫 `$"logs-{DateTime.UtcNow:yyyy.MM.dd}"`） | **完全不用**（永遠指向固定 Data Stream） |
| **Document 資料需求** | 純 JSON 欄位 | JSON 欄位中**必須包含 `@timestamp`** |
| **分片（Shard）均勻度** | 差（量少時 Shard 過小浪費記憶體，暴量時 Shard 過大） | **優**（由 ILM 嚴格依照「每滿 40GB」自動 Rollover 切分） |
| **生命週期自動清理** | 需額外寫外部 CronJob / 腳本定期掃描刪除 | **內建自動化**（由 ILM 策略設定滿 30 天底層自動刪除） |

---

#### 4. Document 資料需求與寫入規範
1. **欄位結構（Mapping）已事先固化**：
   * 在 Index Template 中已經定義了 `service (keyword)`、`message (text)`、`level (keyword)` 等型態，寫入 Document 時不需要在 Header 或 Body 重複聲明結構。
2. **必備 `@timestamp` 欄位**：
   * 寫入 Document 時，只要在 JSON 本體包含 `@timestamp`（ISO 8601 UTC 時間字串，如 `"2026-08-23T17:30:00.000Z"`），Elasticsearch 就會依據該時間戳記，在底層自動分流到當前啟用的 Backing Index 中。


---

### 4.2 初始化 ILM 與 Data Stream 腳本

```bash
# 1. 建立 ILM 策略 (滿 40GB 或 1 天 Rollover，30 天後刪除)
curl -X PUT "http://localhost:9200/_ilm/policy/logs_ilm_policy" \
  -H "Content-Type: application/json" \
  -d '{
    "policy": {
      "phases": {
        "hot": {
          "actions": {
            "rollover": {
              "max_primary_shard_size": "40gb",
              "max_age": "1d"
            }
          }
        },
        "delete": {
          "min_age": "30d",
          "actions": {
            "delete": {}
          }
        }
      }
    }
  }'

# 2. 建立 Index Template 綁定 Data Stream
curl -X PUT "http://localhost:9200/_index_template/logs_template" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["logs-app-*"],
    "data_stream": { },
    "template": {
      "settings": {
        "index.lifecycle.name": "logs_ilm_policy",
        "index.number_of_shards": 2,
        "index.number_of_replicas": 1,
        "index.refresh_interval": "10s"
      },
      "mappings": {
        "properties": {
          "@timestamp": { "type": "date" },
          "service": { "type": "keyword" },
          "level": { "type": "keyword" },
          "message": { "type": "text" },
          "traceId": { "type": "keyword" }
        }
      }
    },
    "priority": 500
  }'
```

---

## 5. ASP.NET Core 10 Web API 實作

專案完整原始碼位於 `src/EsDailyLogsApi`。請安裝 SDK 8.17.0：`dotnet add package Elastic.Clients.Elasticsearch --version 8.17.0`。

### 核心實作摘要
- **記憶體非阻塞佇列 (`LogQueue.cs`)**：利用 `System.Threading.Channels` 接收高併發的 Log 請求，API 耗時 < 1ms。
- **背景批次寫入 (`LogBatchProcessor.cs`)**：從 Queue 讀取並使用 `BulkAsync` 批次寫入固定的 Data Stream 端點（`logs-app-prod`）。
- **CRUD 服務 (`LogService.cs`)**：處理查詢、更新與刪除操作。
- **Minimal API (`Program.cs`)**：
  ```csharp
  // 寫入 Log (快速推入 Queue 並回傳 202 Accepted，不需指定日期)
  app.MapPost("/api/logs", async (LogEntry entry, ILogQueue queue) => {
      entry.Timestamp = DateTime.UtcNow;
      await queue.EnqueueAsync(entry);
      return Results.Accepted();
  });
  ```

---

## 6. 端對端驗證與自動化測試

本解決方案包含兩種驗證方式：**xUnit 自動化測試專案** 與 **Bash 端對端呼叫腳本**。

### 6.1 執行 xUnit 自動化測試專案

解決方案中包含 [`tests/EsDailyLogsApi.Tests`](file:///mnt/d/lab/survey-elasticsearch/tests/EsDailyLogsApi.Tests)：
* **`LogQueueTests.cs`**：單元測試，驗證 `System.Threading.Channels` 的非阻塞寫入與讀取。
* **`LogServiceIntegrationTests.cs`**：整合測試，對 Elasticsearch 實際執行 Data Stream 下完整的 CRUD 生命週期驗證。
* **`LogApiIntegrationTests.cs`**：Web API 整合測試，使用 `WebApplicationFactory<Program>` 驗證 HTTP API 端點。

執行測試指令：
```bash
dotnet test EsDailyLogs.slnx
```

**🖥️ 測試執行通過畫面：**
```text
Test run for tests/EsDailyLogsApi.Tests/bin/Debug/net10.0/EsDailyLogsApi.Tests.dll (.NETCoreApp,Version=v10.0)
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 579 ms
```

---

### 6.2 執行 Bash 模擬腳本

執行本專案提供的測試腳本 [`test_api.sh`](file:///mnt/d/lab/survey-elasticsearch/test_api.sh)：

```bash
# 1. 啟動 Web API
dotnet run --project src/EsDailyLogsApi/EsDailyLogsApi.csproj

# 2. 在另一個終端機執行測試腳本
./test_api.sh
```

實測支援完整 CRUD：成功將資料推入 Queue (202)、搜尋紀錄 (200)、更新/刪除指定的底層文件 (204)。

---

## 7. 維運避坑指南與最佳實踐

### 7.1 分片容量規劃（Shard Sizing）與配置方式

「單個 Shard 維持在 **10 GB ~ 50 GB**」是 Elasticsearch 官方的核心架構準則，ES 本身並沒有單一開關限制 Shard 容量，而是**透過以下配置方式達成**：

#### 1. 時序資料（Logs / Metrics）👉 透過 ILM 自動控制（推薦）
在 ILM 策略中配置 `max_primary_shard_size: "40gb"`，搭配 Index Template 設定 `index.number_of_shards: 2`：
```json
PUT _ilm/policy/logs_ilm_policy
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {
          "rollover": {
            "max_primary_shard_size": "40gb", // 當任一 Primary Shard 達 40GB 自動 Rollover
            "max_age": "1d"
          }
        }
      }
    }
  }
}
```
* **效果**：每當資料累積約 80GB（2 Shards × 40GB）或滿 1 天時，ES 會自動切換至下一個新索引，由系統自動保證分片永遠落在最佳效能區間。

#### 2. 靜態業務資料（商品、使用者資料庫）👉 透過預估容量配置
* **計算公式**：`Primary Shards 數量 = 預估資料總量 / 30GB`
* **範例**：預估總資料量 60GB，設定 `number_of_shards: 2`（單個 Shard 約 30GB）。

#### 3. 記憶體（JVM Heap）與分片比率檢核
* **經驗法則**：節點每 **1 GB JVM Heap** 承載的分片數量**不應超過 20 個**（例如：配置 31GB Heap 的節點，單節點分片總數建議小於 600 個），避免 Over-sharding 耗盡記憶體。

---

### 7.2 官方參考文件連結（Official References）

| 規範主題 | 官方文件說明 | 官方參考連結 |
|---|---|---|
| **分片容量規劃** | Size your shards (How many shards should I have?) | [Elastic Docs: Size your shards](https://www.elastic.co/guide/en/elasticsearch/reference/current/size-your-shards.html) |
| **避免過度分片** | Avoid oversharding (Capacity planning & Heap usage) | [Elastic Docs: Avoid oversharding](https://www.elastic.co/guide/en/elasticsearch/reference/current/avoid-oversharding.html) |
| **ILM Rollover 動作** | Index Lifecycle Management: Rollover action | [Elastic Docs: ILM Rollover](https://www.elastic.co/guide/en/elasticsearch/reference/current/ilm-rollover.html) |
| **Data Streams 概念** | Set up a data stream & Backing indices | [Elastic Docs: Data streams](https://www.elastic.co/guide/en/elasticsearch/reference/current/data-streams.html) |

---

### 7.3 其他核心維運避坑點

1. **Client / Server 版本相容性**：若 Elasticsearch Server 為 `8.17.x`，NuGet 請務必鎖定 `Elastic.Clients.Elasticsearch` `8.17.0`；若安裝 `9.x` 會因 `compatible-with=9` 標頭造成 Server 拒絕請求（400 Bad Request）。
2. **高吞吐寫入 Refresh Interval**：預設 `1s` 頻繁建立 Segment 損耗 CPU，海量寫入建議調整為 `10s` 或 `30s`。
3. **Data Stream 下的 Update / Delete**：Data Stream 本質為 Append-only；若有稽核修正或個資去識別化需求，需取得底層 Backing Index 名稱（如 `.ds-logs-app-prod-2026.08.23-000001`）與 `_id` 進行操作。
4. **備份與快照（Snapshot）**：Replica 僅提供節點容錯與查詢分流，無法防止誤刪操作；生產環境必須設定 SLM 定時快照至外部雲端儲存（S3/GCS/NFS）。

