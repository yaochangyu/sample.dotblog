# Elasticsearch 可觀測性整合實作計畫

> **計畫建立日期**：2026-02-10
> **目標**：整合 Elasticsearch 作為可觀測性資料的中心儲存，並透過 Kibana 視覺化 Traces、Logs、Metrics

---

## 1. 架構概覽

### 1.1 目標架構

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Nuxt 4     │────▶│   backend-a  │────▶│   backend-b  │
│  (Frontend)  │     │ ASP.NET 10   │     │ ASP.NET 10   │
└──────────────┘     └──────┬───────┘     └──────┬───────┘
                            │                    │
                     Serilog│+ OTel SDK   Serilog│+ OTel SDK
                            │                    │
                            ▼                    ▼
                    ┌───────────────────────────────┐
                    │      OTel Collector (OTLP)    │
                    └───┬───────────┬───────────────┘
                        │           │
                        │           └──────────────┐
                        ▼                          ▼
                ┌───────────────┐          ┌─────────┐
                │ Elasticsearch │          │   Seq   │
                │   (中心儲存)   │          │  (保留)  │
                └───┬───────┬───┘          └─────────┘
                    │       │                    ▲
        ┌───────────┘       └───────────┐        │
        ▼                               ▼        │
┌───────────────┐                ┌─────────────┐ │
│    Jaeger     │                │   Kibana    │ │
│  All-in-One   │                │  Dashboard  │ │
│ (從 ES 讀取)   │                │ (Logs/Traces│ │
└───────────────┘                │  /Metrics)  │ │
                                 └─────────────┘ │
┌─────────────────┐                              │
│ Aspire Dashboard│ ← 維持從 OTel Collector  ─────┘
│  (即時資料流)     │    接收即時資料
└─────────────────┘
```

### 1.2 資料流向

| 訊號類型 | 來源 | 路徑 | 目的地 |
|---------|------|------|--------|
| **Traces** | Frontend (Browser) | OTLP HTTP → OTel Collector | → Elasticsearch → Jaeger<br>→ Aspire Dashboard (即時) |
| **Traces** | Backend-A/B | OTLP gRPC → OTel Collector | → Elasticsearch → Jaeger<br>→ Aspire Dashboard (即時) |
| **Logs** | Backend-A/B (Serilog) | 1) OTLP gRPC → OTel Collector<br>2) Serilog Seq Sink (直連) | → Elasticsearch → Kibana<br>→ Seq<br>→ Aspire Dashboard (即時) |
| **Metrics** | Backend-A/B | OTLP gRPC → OTel Collector | → Elasticsearch → Kibana<br>→ Aspire Dashboard (即時) |

### 1.3 關鍵設計決策

#### ✅ 保留 Aspire Dashboard 的原因
- **技術限制**：Aspire Dashboard 預設不支援從 Elasticsearch 讀取歷史資料
- **解決方案**：
  - Aspire Dashboard 維持從 OTel Collector 接收**即時資料流** (OTLP)
  - Elasticsearch + Kibana 負責**歷史資料查詢**和**長期儲存**
  - Jaeger 提供**分散式追蹤視覺化**（從 Elasticsearch 讀取）

#### ✅ Seq 保留策略
- Seq 繼續接收 Serilog 日誌（透過 Seq Sink）
- 同時日誌也透過 OTel Collector 寫入 Elasticsearch
- **原因**：提供多重日誌查詢途徑，避免單點故障

---

## 2. 新增/修改服務列表

### 2.1 新增服務

| 服務名稱 | 映像 | 本機端口 | 容器端口 | 用途 |
|---------|------|---------|---------|------|
| **elasticsearch** | `docker.elastic.co/elasticsearch/elasticsearch:8.17.1` | 9200 | 9200 | 中心儲存（Traces/Logs/Metrics） |
| **kibana** | `docker.elastic.co/kibana/kibana:8.17.1` | 5601 | 5601 | 資料視覺化與查詢 UI |

### 2.2 修改服務

| 服務名稱 | 修改項目 | 原因 |
|---------|---------|------|
| **otel-collector** | 新增 `elasticsearch` exporter 配置 | 將 Traces/Logs/Metrics 寫入 ES |
| **jaeger** | 切換為使用 Elasticsearch 儲存後端 | 從 ES 讀取 Traces 資料 |
| **backend-a / backend-b** | 無需修改程式碼 | OTel SDK 配置已足夠 |
| **frontend** | 無需修改程式碼 | OTel Browser SDK 配置已足夠 |

### 2.3 保留服務（無修改）

- **aspire-dashboard**：維持從 OTel Collector 接收即時資料
- **seq**：維持接收 Serilog 日誌

---

## 3. 實作步驟

### 階段一：Elasticsearch 基礎建設

- [x] **步驟 1：建立 Elasticsearch 服務配置**
  - **目的**：部署 Elasticsearch 作為中心儲存
  - **檔案**：`docker-compose.yml`
  - **配置內容**：
    - 記憶體限制：4GB (heap size: 2GB)
    - 單節點模式 (discovery.type: single-node)
    - 關閉 X-Pack Security（開發環境）
    - Volume 掛載：`./data/elasticsearch:/usr/share/elasticsearch/data`
  - **端口**：9200
  - **依賴**：無

- [x] **步驟 2：建立 Kibana 服務配置**
  - **目的**：提供 Elasticsearch 資料的視覺化介面
  - **檔案**：`docker-compose.yml`
  - **配置內容**：
    - 連接 Elasticsearch：`http://elasticsearch:9200`
    - 語言設定：繁體中文 (zh-TW)
  - **端口**：5601
  - **依賴**：elasticsearch

- [x] **步驟 3：啟動並驗證 Elasticsearch + Kibana**
  - **目的**：確認基礎服務正常運作
  - **驗證方式**：
    ```bash
    # 檢查 Elasticsearch 健康狀態
    curl http://localhost:9200/_cluster/health

    # 檢查 Kibana 是否可訪問
    curl http://localhost:5601/api/status
    ```
  - **預期結果**：兩者皆返回 200 OK
  - **依賴**：步驟 1、步驟 2

---

### 階段二：OTel Collector 整合

- [x] **步驟 4：配置 OTel Collector Elasticsearch Exporter**
  - **目的**：將 Traces、Logs、Metrics 寫入 Elasticsearch
  - **檔案**：`data/otel-collector/otel-collector-config.yaml`
  - **配置內容**：
    ```yaml
    exporters:
      elasticsearch:
        endpoints: ["http://elasticsearch:9200"]
        logs_index: "otel-logs"
        traces_index: "otel-traces"
        metrics_index: "otel-metrics"
        mapping:
          mode: ecs  # 使用 Elastic Common Schema
    ```
  - **管道修改**：
    - traces: 新增 `elasticsearch` 到 exporters 列表
    - logs: 新增 `elasticsearch` 到 exporters 列表
    - metrics: 新增 `elasticsearch` 到 exporters 列表
  - **依賴**：步驟 3
  - **完成時間**：2026-02-10

- [x] **步驟 5：更新 OTel Collector 服務依賴**
  - **目的**：確保 OTel Collector 在 Elasticsearch 啟動後才啟動
  - **檔案**：`docker-compose.yml`
  - **修改內容**：
    ```yaml
    otel-collector:
      depends_on:
        - jaeger
        - aspire-dashboard
        - elasticsearch  # ← 新增
    ```
  - **依賴**：步驟 3
  - **完成時間**：2026-02-10

- [x] **步驟 6：重啟 OTel Collector 並驗證資料寫入**
  - **目的**：確認資料正確寫入 Elasticsearch
  - **驗證方式**：
    ```bash
    # 執行測試請求（GET /api/weather）
    curl http://localhost:3000/api/weather

    # 檢查 Elasticsearch 索引是否建立
    curl http://localhost:9200/_cat/indices?v

    # 查看 traces 資料
    curl http://localhost:9200/otel-traces/_search?size=1
    ```
  - **預期結果**：看到 `otel-traces`, `otel-logs`, `otel-metrics` 索引
  - **依賴**：步驟 4、步驟 5
  - **完成時間**：2026-02-10
  - **驗證結果**：
    - ✅ `otel-traces`: 3 筆 traces（ECS 格式）
    - ✅ `otel-logs`: 56 筆 logs
    - ✅ 資料結構正確，包含 trace_id、span_id、service_name 等欄位

---

### 階段三：Jaeger 整合 Elasticsearch

- [x] **步驟 7：修改 Jaeger 服務配置**
  - **目的**：將 Jaeger 儲存後端切換為 Elasticsearch
  - **檔案**：`docker-compose.yml`
  - **配置內容**：
    ```yaml
    jaeger:
      image: jaegertracing/all-in-one:latest
      environment:
        - SPAN_STORAGE_TYPE=elasticsearch
        - ES_SERVER_URLS=http://elasticsearch:9200
        - ES_INDEX_PREFIX=jaeger
        - ES_VERSION=8
      depends_on:
        - elasticsearch
    ```
  - **移除內容**：移除記憶體儲存相關的環境變數（如有）
  - **依賴**：步驟 3
  - **完成時間**：2026-02-10
  - **備註**：暫時未啟用 ES_USE_ILM，將在階段四單獨配置 ILM 策略

- [x] **步驟 8：建立 Jaeger Elasticsearch Index Template**
  - **目的**：確保 Jaeger 的 Span 資料結構正確
  - **方式**：Jaeger 會在啟動時自動建立索引模板
  - **驗證方式**：
    ```bash
    # 檢查 Jaeger 索引
    curl http://localhost:9200/_cat/indices?v | grep jaeger
    ```
  - **預期結果**：看到 `jaeger-span-*` 和 `jaeger-service-*` 索引
  - **依賴**：步驟 7
  - **完成時間**：2026-02-10
  - **驗證結果**：
    - ✅ `jaeger-jaeger-span-2026-02-10` 索引已建立
    - ✅ `jaeger-jaeger-service-2026-02-10` 索引已建立

- [x] **步驟 9：驗證 Jaeger UI 可查詢 Elasticsearch 資料**
  - **目的**：確認 Jaeger 正確讀取 Elasticsearch 中的 Traces
  - **驗證方式**：
    1. 訪問 http://localhost:16686
    2. 選擇 Service: `frontend` 或 `backend-a`
    3. 點擊 "Find Traces"
  - **預期結果**：顯示完整的 Trace 鏈路
  - **依賴**：步驟 8
  - **完成時間**：2026-02-10
  - **驗證結果**：
    - ✅ Jaeger UI 可查詢服務：backend-a, backend-b
    - ✅ Trace 資料完整（traceID: 3f072ef604ac1310a073dc205650201d, 3 個 spans）
    - ✅ 涵蓋 2 個 processes（backend-a 和 backend-b）

---

### 階段四：Elasticsearch Index Lifecycle Management (ILM)

- [ ] **步驟 10：建立 ILM 策略腳本**
  - **目的**：自動管理索引生命週期（14 天後刪除）
  - **檔案**：`scripts/setup-elasticsearch-ilm.sh`
  - **腳本內容**：
    ```bash
    #!/bin/bash
    # 建立 ILM 策略：14 天後刪除
    curl -X PUT "http://localhost:9200/_ilm/policy/otel-14day-policy" \
      -H 'Content-Type: application/json' -d'
    {
      "policy": {
        "phases": {
          "hot": {
            "actions": {
              "rollover": {
                "max_age": "1d",
                "max_size": "50gb"
              }
            }
          },
          "delete": {
            "min_age": "14d",
            "actions": {
              "delete": {}
            }
          }
        }
      }
    }'

    # 套用到 OTel 索引
    curl -X PUT "http://localhost:9200/_index_template/otel-logs-template" \
      -H 'Content-Type: application/json' -d'
    {
      "index_patterns": ["otel-logs-*"],
      "template": {
        "settings": {
          "index.lifecycle.name": "otel-14day-policy",
          "index.lifecycle.rollover_alias": "otel-logs"
        }
      }
    }'

    # 對 traces 和 metrics 執行相同操作
    # ...（省略，實作時補充）
    ```
  - **依賴**：步驟 6

- [ ] **步驟 11：執行 ILM 設定腳本**
  - **目的**：套用 ILM 策略到 Elasticsearch
  - **執行方式**：
    ```bash
    chmod +x scripts/setup-elasticsearch-ilm.sh
    ./scripts/setup-elasticsearch-ilm.sh
    ```
  - **驗證方式**：
    ```bash
    # 檢查 ILM 策略
    curl http://localhost:9200/_ilm/policy/otel-14day-policy
    ```
  - **預期結果**：返回策略配置 JSON
  - **依賴**：步驟 10

---

### 階段五：Kibana Dashboard 建立

- [ ] **步驟 12：建立 Kibana Index Patterns**
  - **目的**：讓 Kibana 識別 Elasticsearch 中的資料索引
  - **方式**：透過 Kibana UI 或 API
  - **建立項目**：
    - `otel-traces-*`：Traces 資料
    - `otel-logs-*`：Logs 資料
    - `otel-metrics-*`：Metrics 資料
    - `jaeger-span-*`：Jaeger Spans 資料
  - **時間欄位**：`@timestamp`
  - **依賴**：步驟 6、步驟 8

- [ ] **步驟 13：匯入預設 Kibana Dashboard**
  - **目的**：提供開箱即用的視覺化介面
  - **檔案**：`data/kibana/dashboards/otel-overview.ndjson`
  - **Dashboard 內容**：
    1. **Traces Overview**：
       - 請求數量趨勢圖
       - 服務延遲分佈圖
       - Top 10 慢查詢
       - Trace 狀態碼分佈
    2. **Logs Overview**：
       - 日誌等級分佈（INFO/WARN/ERROR）
       - 錯誤日誌 Top 10
       - 依服務分組的日誌量
    3. **Metrics Overview**：
       - HTTP 請求數/秒
       - CPU/Memory 使用率（如有 instrumentation）
  - **匯入方式**：
    ```bash
    curl -X POST "http://localhost:5601/api/saved_objects/_import" \
      -H "kbn-xsrf: true" \
      --form file=@data/kibana/dashboards/otel-overview.ndjson
    ```
  - **依賴**：步驟 12

- [ ] **步驟 14：建立 Kibana Discover 查詢樣板**
  - **目的**：提供常用查詢的快速入口
  - **建立項目**：
    1. **錯誤日誌查詢**：`level: ERROR`
    2. **特定 TraceId 查詢**：`trace_id: "xxxxx"`
    3. **慢請求查詢**：`duration > 1000`（單位：毫秒）
  - **儲存位置**：Kibana Discover → Save Search
  - **依賴**：步驟 12

---

### 階段六：整合測試與驗證

- [ ] **步驟 15：建立端到端測試腳本**
  - **目的**：自動化驗證整個資料流
  - **檔案**：`scripts/verify-elasticsearch-integration.sh`
  - **測試項目**：
    1. 發送測試請求（GET /api/weather）
    2. 等待 5 秒（資料寫入延遲）
    3. 檢查 Elasticsearch 是否有新資料
    4. 檢查 Jaeger UI 是否能查詢到 Trace
    5. 檢查 Kibana 是否能查詢到 Logs
    6. 檢查 Aspire Dashboard 是否顯示即時資料
    7. 檢查 Seq 是否收到日誌
  - **執行方式**：
    ```bash
    chmod +x scripts/verify-elasticsearch-integration.sh
    ./scripts/verify-elasticsearch-integration.sh
    ```
  - **依賴**：步驟 13

- [ ] **步驟 16：建立壓力測試腳本**
  - **目的**：驗證系統在高負載下的穩定性
  - **檔案**：`scripts/load-test.sh`
  - **測試方式**：使用 `ab` (Apache Bench) 或 `wrk`
    ```bash
    # 發送 1000 個請求，併發 10
    ab -n 1000 -c 10 http://localhost:3000/api/weather
    ```
  - **驗證項目**：
    - Elasticsearch 索引大小增長
    - OTel Collector CPU/Memory 使用率
    - Jaeger 查詢效能
  - **依賴**：步驟 15

- [ ] **步驟 17：更新 README 文檔**
  - **目的**：記錄新架構的使用方式
  - **檔案**：`README.md` 或新建 `docs/elasticsearch-integration.md`
  - **文檔內容**：
    - 架構圖
    - 服務端口列表
    - 啟動指令
    - 常見問題排查
    - Kibana Dashboard 使用說明
  - **依賴**：步驟 16

---

## 4. 技術細節

### 4.1 Elasticsearch 配置

```yaml
# docker-compose.yml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.17.1
  container_name: elasticsearch
  environment:
    - discovery.type=single-node
    - xpack.security.enabled=false
    - "ES_JAVA_OPTS=-Xms2g -Xmx2g"  # Heap size: 2GB (總記憶體 4GB)
    - bootstrap.memory_lock=true
  ulimits:
    memlock:
      soft: -1
      hard: -1
  volumes:
    - ./data/elasticsearch:/usr/share/elasticsearch/data
  ports:
    - "9200:9200"
  networks:
    - opentelemetry-lab
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:9200/_cluster/health || exit 1"]
    interval: 30s
    timeout: 10s
    retries: 5
```

### 4.2 OTel Collector Elasticsearch Exporter

```yaml
# data/otel-collector/otel-collector-config.yaml
exporters:
  elasticsearch:
    endpoints: ["http://elasticsearch:9200"]
    logs_index: "otel-logs"
    traces_index: "otel-traces"
    metrics_index: "otel-metrics"
    sending_queue:
      enabled: true
      num_consumers: 10
      queue_size: 1000
    retry_on_failure:
      enabled: true
      initial_interval: 5s
      max_interval: 30s
      max_elapsed_time: 300s
    mapping:
      mode: ecs  # Elastic Common Schema

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/jaeger, otlp/aspire, elasticsearch]  # ← 新增

    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/aspire, elasticsearch]  # ← 新增

    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/aspire, elasticsearch]  # ← 新增
```

### 4.3 Jaeger Elasticsearch 配置

```yaml
# docker-compose.yml
jaeger:
  image: jaegertracing/all-in-one:latest
  container_name: jaeger
  environment:
    - SPAN_STORAGE_TYPE=elasticsearch
    - ES_SERVER_URLS=http://elasticsearch:9200
    - ES_INDEX_PREFIX=jaeger
    - ES_VERSION=8
    - ES_USE_ILM=true  # 啟用 ILM（需手動設定策略）
    - COLLECTOR_OTLP_ENABLED=true
  ports:
    - "16686:16686"  # Jaeger UI
    - "4317:4317"    # OTLP gRPC (保留)
    - "4318:4318"    # OTLP HTTP (保留)
  depends_on:
    - elasticsearch
  networks:
    - opentelemetry-lab
```

### 4.4 Kibana 配置

```yaml
# docker-compose.yml
kibana:
  image: docker.elastic.co/kibana/kibana:8.17.1
  container_name: kibana
  environment:
    - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    - I18N_LOCALE=zh-TW
    - XPACK_SECURITY_ENABLED=false
  ports:
    - "5601:5601"
  depends_on:
    - elasticsearch
  networks:
    - opentelemetry-lab
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:5601/api/status || exit 1"]
    interval: 30s
    timeout: 10s
    retries: 5
```

---

## 5. 端口對照表（更新版）

| 服務 | 本機端口 | 容器端口 | 用途 |
|------|---------|---------|------|
| frontend | 3000 | 3000 | Nuxt 前端 |
| backend-a | 5100 | 8080 | ASP.NET API-A |
| backend-b | 5200 | 8080 | ASP.NET API-B |
| otel-collector | 4317, 4318 | 4317, 4318 | OTLP Receiver |
| jaeger | 16686 | 16686 | Jaeger UI |
| seq | 5341 | 80 | Seq UI |
| aspire-dashboard | 18888 | 18888 | Aspire Dashboard |
| **elasticsearch** | **9200** | **9200** | Elasticsearch API |
| **kibana** | **5601** | **5601** | Kibana UI |

---

## 6. 驗收標準

### 6.1 功能性驗收

| 項目 | 驗證方式 | 預期結果 |
|------|---------|---------|
| **Elasticsearch 運作** | `curl http://localhost:9200/_cluster/health` | status: green 或 yellow |
| **Kibana 可訪問** | 瀏覽器訪問 http://localhost:5601 | 顯示 Kibana 首頁 |
| **OTel Collector 寫入 ES** | `curl http://localhost:9200/_cat/indices?v` | 看到 `otel-traces`, `otel-logs`, `otel-metrics` |
| **Jaeger 讀取 ES** | Jaeger UI 查詢 Traces | 顯示完整 Trace 鏈路 |
| **Kibana Dashboard** | 訪問 Kibana Dashboard | 顯示 Traces/Logs/Metrics 視覺化 |
| **ILM 策略套用** | `curl http://localhost:9200/_ilm/policy/otel-14day-policy` | 返回策略配置 |
| **Seq 正常運作** | 訪問 http://localhost:5341 | 顯示日誌（保持原有功能） |
| **Aspire Dashboard** | 訪問 http://localhost:18888 | 顯示即時 Traces/Logs/Metrics |

### 6.2 效能驗收

| 項目 | 指標 | 預期值 |
|------|------|--------|
| Elasticsearch Heap 使用率 | JVM Heap Used | < 80% |
| OTel Collector CPU | Container CPU | < 50% (閒置), < 80% (高負載) |
| Trace 查詢延遲 | Jaeger UI 查詢時間 | < 3 秒 |
| Kibana 查詢延遲 | Discover 頁面載入時間 | < 5 秒 |

### 6.3 完整追蹤鏈驗收

**測試流程**：
1. 瀏覽器訪問 http://localhost:3000
2. 點擊「取得天氣」按鈕（發送 GET /api/weather）
3. 等待 5 秒

**驗證點**：
- [ ] Jaeger UI 可查到 `frontend → backend-a → backend-b` 的完整 Trace
- [ ] Kibana Discover 可搜尋到對應的日誌（使用 TraceId 過濾）
- [ ] Seq 可查到 backend-a 和 backend-b 的日誌
- [ ] Aspire Dashboard 顯示即時的 Trace 和 Logs
- [ ] Elasticsearch 索引包含 Trace、Log、Metric 資料

---

## 7. 潛在風險與應對

| 風險 | 影響 | 應對措施 |
|------|------|---------|
| **Elasticsearch 記憶體不足** | 服務無法啟動 | 降低 Heap Size 或增加主機記憶體 |
| **OTel Collector 寫入失敗** | 資料遺失 | 檢查 Collector logs，確認 ES endpoint 正確 |
| **Jaeger 無法連接 ES** | Trace 查詢失敗 | 檢查 `SPAN_STORAGE_TYPE` 和 `ES_SERVER_URLS` |
| **Kibana Index Pattern 未建立** | 無法查詢資料 | 手動建立或透過 API 自動建立 |
| **ILM 策略未生效** | 資料無法自動刪除 | 檢查索引是否套用 ILM，確認 rollover alias |

---

## 8. 後續優化建議

- [ ] **Elasticsearch 集群化**：開發環境若需要高可用，考慮部署 3 節點集群
- [ ] **Kibana Alert 規則**：設定告警規則（如錯誤日誌超過閾值）
- [ ] **APM 整合**：考慮使用 Elastic APM 取代部分 OTel Instrumentation
- [ ] **Grafana 整合**：若需要更靈活的 Dashboard，可整合 Grafana + Elasticsearch datasource
- [ ] **效能調校**：根據實際資料量調整 OTel Collector batch size 和 ES refresh interval

---

## 9. 參考文件

- [OpenTelemetry Collector Elasticsearch Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/elasticsearchexporter)
- [Jaeger Elasticsearch Storage](https://www.jaegertracing.io/docs/latest/deployment/#elasticsearch)
- [Elasticsearch Index Lifecycle Management (ILM)](https://www.elastic.co/guide/en/elasticsearch/reference/current/index-lifecycle-management.html)
- [Kibana Dashboard API](https://www.elastic.co/guide/en/kibana/current/dashboard-api.html)
- [Elastic Common Schema (ECS)](https://www.elastic.co/guide/en/ecs/current/index.html)

---

## 10. 計畫時間估算

| 階段 | 預估時間 | 說明 |
|------|---------|------|
| 階段一：Elasticsearch 基礎建設 | 1 小時 | Docker Compose 配置 + 驗證 |
| 階段二：OTel Collector 整合 | 1.5 小時 | Exporter 配置 + 測試 |
| 階段三：Jaeger 整合 | 1 小時 | 切換儲存後端 + 驗證 |
| 階段四：ILM 設定 | 1 小時 | 腳本撰寫 + 策略套用 |
| 階段五：Kibana Dashboard | 2 小時 | Index Pattern + Dashboard 建立 |
| 階段六：整合測試 | 1.5 小時 | 測試腳本 + 文檔更新 |
| **總計** | **8 小時** | 約 1 個工作日 |

---

## 附錄：快速啟動指令

```bash
# 1. 啟動所有服務
docker-compose up -d

# 2. 等待 Elasticsearch 健康檢查通過
docker-compose ps elasticsearch

# 3. 設定 ILM 策略
./scripts/setup-elasticsearch-ilm.sh

# 4. 匯入 Kibana Dashboard
./scripts/import-kibana-dashboards.sh

# 5. 驗證整合
./scripts/verify-elasticsearch-integration.sh

# 6. 訪問服務
# - Frontend: http://localhost:3000
# - Jaeger UI: http://localhost:16686
# - Kibana: http://localhost:5601
# - Seq: http://localhost:5341
# - Aspire Dashboard: http://localhost:18888
# - Elasticsearch: http://localhost:9200
```

---

**計畫書版本**：v1.0
**建立者**：DevOps Team
**審核狀態**：待審核 ⏳
