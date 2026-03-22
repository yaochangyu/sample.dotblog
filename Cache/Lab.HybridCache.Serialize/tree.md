# 專案資料夾結構

```
Lab.HybridCache.Serialize/
├── Lab.HybridCache.Serialize.slnx             # 方案檔
├── docker-compose.yml                          # Redis docker compose 設定
├── tree.md                                     # 專案資料夾結構
├── redis-serialization-benchmark.plan.md       # 序列化格式比較報告（MessagePack/MemoryPack/LZ4）
├── hybridcache-serializer-report.md            # HybridCache L2 序列化器比較報告（自動產生）
└── Lab.HybridCache.Serialize/
    ├── Lab.HybridCache.Serialize.csproj        # 專案檔
    ├── Lab.HybridCache.Serialize.http          # HTTP 測試檔
    ├── Program.cs                              # 應用程式進入點，註冊 Redis、HybridCache、Controllers
    ├── appsettings.json                        # 設定檔（含 Redis 連線字串）
    ├── appsettings.Development.json            # 開發環境設定
    ├── Properties/
    │   └── launchSettings.json                 # 啟動設定
    ├── Models/
    │   └── ProductModel.cs                     # 測試用資料模型（MessagePack + MemoryPack）
    ├── Serializers/
    │   ├── MessagePackHybridCacheSerializer.cs # HybridCache L2 序列化器（MessagePack）
    │   └── MemoryPackHybridCacheSerializer.cs  # HybridCache L2 序列化器（MemoryPack）
    └── Controllers/
        ├── BenchmarkController.cs              # 序列化比較端點
        ├── HybridCacheController.cs            # HybridCache GetOrCreate / Evict 端點
        └── HybridCacheBenchmarkController.cs   # HybridCache L2 序列化器比較端點（輸出 .md 報告）
```

## API 端點

| Method | Path | 說明 |
|--------|------|------|
| POST | `/benchmark/write/{id}` | 以 4 種格式（msgpack、msgpack-lz4、mempack、mempack-lz4）寫入單筆資料 |
| GET  | `/benchmark/stats/{id}` | 比較單筆資料 4 種格式的 byte 大小與壓縮率 |
| GET  | `/benchmark/speed/{count}` | 4 種格式（含 LZ4）序列化速度比較 |
| POST | `/benchmark/batch/{count}` | 批次寫入 msgpack/mempack 並回傳彙總統計 |
| GET  | `/benchmark/storage` | 統計 4 種格式（含 LZ4）所有 key 在 Redis 中佔用的實際記憶體 |
| GET  | `/hybridcache/{id}` | 以 HybridCache GetOrCreate 取得商品（L1 → L2 → factory） |
| DELETE | `/hybridcache/{id}` | 移除指定 id 的 HybridCache 快取（L1 + L2） |
| POST | `/HybridCacheBenchmark/run/{count}` | 執行 MessagePack vs MemoryPack L2 比較，輸出 hybridcache-serializer-report.md |
