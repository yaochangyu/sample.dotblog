# Redis Serialization Benchmark Plan

## 目標
比較 MessagePack 與 MemoryPack 序列化後寫入 Redis 所占用的空間大小。

## 步驟

- [ ] **步驟 1：建立 ASP.NET Core 10 Web API 專案**
  - 使用 `dotnet new webapi` 建立專案
  - 確認 .NET 10 SDK

- [ ] **步驟 2：建立 docker-compose.yml**
  - 加入 Redis 服務（官方映像）
  - 設定 port mapping 6379:6379

- [ ] **步驟 3：安裝 NuGet 套件**
  - `MessagePack` — MessagePack 序列化
  - `MemoryPack` — MemoryPack 序列化
  - `StackExchange.Redis` — Redis 客戶端

- [ ] **步驟 4：定義測試用資料模型**
  - 建立同一個 POCO，分別標注 `[MessagePackObject]` 與 `[MemoryPackable]`
  - 模型包含多種型別欄位（string、int、DateTime、List）以模擬真實場景

- [ ] **步驟 5：實作 BenchmarkController**
  - 提供 POST `/benchmark/write` 端點
  - 將同一筆資料分別用 MessagePack、MemoryPack 序列化後寫入 Redis
  - Key 命名：`msgpack:{id}`、`mempack:{id}`

- [ ] **步驟 6：實作 GET `/benchmark/stats` 端點**
  - 從 Redis 讀取兩種 key 的 `STRLEN`（byte 大小）
  - 回傳比較結果（含壓縮比）

- [ ] **步驟 7：更新 tree.md**
  - 記錄最終資料夾結構
