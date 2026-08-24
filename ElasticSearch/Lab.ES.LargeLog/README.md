# Elasticsearch + .NET 10 Web API 小實驗

這個專案在做兩件事：

1. 用 **Data Stream** 寫入日誌。
2. 用 **手動按日索引** 寫入日誌，拿來跟 Data Stream 比較。

你可以把它想成：

- **Data Stream**：把日誌丟進同一個箱子，ES 自己幫你分批。
- **手動按日索引**：每天自己換一個箱子名字。

---

## 專案裡有什麼

- `src/EsDailyLogsApi/Program.cs`：Web API 入口。
- `scripts/run-stress-test.sh`：測 `POST /api/logs`。
- `scripts/run-daily-index-stress-test.sh`：測 `POST /api/daily-index/logs`。
- `scripts/k6-daily-index-logs.js`：給 k6 用的壓測腳本。
- `test_api.sh`：簡單 API 測試腳本。
- `tests/`：自動化測試。

---

## 先準備環境

你需要：

- Docker
- .NET SDK
- k6（腳本會用 Docker 跑 k6）

啟動 Elasticsearch：

```bash
docker compose up -d
```

API 預設連到 `http://localhost:9200`。

---

## 兩個主要 API

### 1) Data Stream 寫入

- `POST /api/logs`

這個會先把資料放進 queue，再批次寫進 Data Stream。

### 2) 手動按日索引寫入

- `POST /api/daily-index/logs`

這個會自己算今天的索引名，像 `logs-app-2026.08.24`。

---

## 怎麼跑

### 跑 API

```bash
dotnet run --project src/EsDailyLogsApi/EsDailyLogsApi.csproj
```

### 跑一般壓測

```bash
chmod +x scripts/run-stress-test.sh
./scripts/run-stress-test.sh
```

### 跑手動按日索引壓測

```bash
chmod +x scripts/run-daily-index-stress-test.sh
./scripts/run-daily-index-stress-test.sh
```

你也可以改時間：

```bash
K6_DURATION=30s ./scripts/run-daily-index-stress-test.sh
```

如果你要往 **1000 萬筆** 目標跑：

```bash
RUN_TO_TARGET_DOCS=1 TARGET_DOCS=10000000 TARGET_RPS=5000 ./scripts/run-daily-index-stress-test.sh
```

這會用單一 RPS 跑到預估的目標時間，然後再檢查 ES 裡的文件數。

如果你要直接跑 **12 小時方案**：

```bash
RUN_PRESET=12h ./scripts/run-daily-index-stress-test.sh
```

---

## 輸出放哪裡

- `run-stress-test.sh`：輸出在 `/tmp/opencode/es-stress-test/<run-id>/`
- `run-daily-index-stress-test.sh`：輸出在 `.output/es-traditional-stress-test/<run-id>/`

`.output/` 已加入 `.gitignore`。

---

## 測試

```bash
dotnet test EsDailyLogs.slnx
```

如果你只想快速看 API 有沒有活著，也可以跑：

```bash
./test_api.sh
```

---

## 這個專案在比較什麼

你可以把它當成一個小對照組：

- **Data Stream**：適合一直寫進來的日誌。
- **手動按日索引**：適合你想自己管每天索引名字的做法。

兩個都能寫、能查、能做壓測。差別是前者比較省事，後者比較像以前的老方法。
