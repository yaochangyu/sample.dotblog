# 專案資料夾結構

```
Lab.HybirdCache.Compress/
├── Lab.HybirdCache.Compress.sln                # 方案檔
├── docker-compose.yml                          # Redis docker compose 設定
├── tree.md                                     # 專案資料夾結構
├── redis-serialization-benchmark.plan.md       # 實作計畫
├── redis-serialization-benchmark.plan.progress.md  # 實作進度
└── Lab.HybirdCache.Compress/
    ├── Lab.HybirdCache.Compress.csproj         # 專案檔
    ├── Lab.HybirdCache.Compress.http           # HTTP 測試檔
    ├── Program.cs                              # 應用程式進入點，註冊 Redis、Controllers
    ├── appsettings.json                        # 設定檔（含 Redis 連線字串）
    ├── appsettings.Development.json            # 開發環境設定
    ├── Properties/
    │   └── launchSettings.json                 # 啟動設定
    ├── Models/
    │   └── ProductModel.cs                     # 測試用資料模型（MessagePack + MemoryPack）
    └── Controllers/
        └── BenchmarkController.cs              # 序列化比較端點
```

## API 端點

| Method | Path | 說明 |
|--------|------|------|
| POST | `/benchmark/write/{id}` | 將單筆資料以兩種格式寫入 Redis |
| GET  | `/benchmark/stats/{id}` | 取得單筆資料的 byte 大小比較 |
| POST | `/benchmark/batch/{count}` | 批次寫入並回傳彙總統計 |
| GET  | `/benchmark/storage` | 統計 4 種格式（含 LZ4）所有 key 在 Redis 中佔用的實際記憶體 |
| POST | `/benchmark/write-compress/{id}` | 以 4 種格式（msgpack、msgpack-lz4、mempack、mempack-lz4）寫入單筆資料 |
| GET  | `/benchmark/stats-compress/{id}` | 比較單筆資料 4 種格式的 byte 大小與壓縮率 |
| GET  | `/benchmark/speed-compress/{count}` | 4 種格式（含 LZ4）序列化速度比較 |
