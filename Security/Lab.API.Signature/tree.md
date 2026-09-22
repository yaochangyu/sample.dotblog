# Lab.API.Signature 專案結構

以下結構排除 `bin/`、`obj/`、`.git/` 與其他執行產物，保留原始碼、測試、Migration、設定與文件。

```text
Lab.API.Signature/
├── README.md                                  # 專案介紹、架構、簽章規格、啟動與風險說明
├── tree.md                                    # 本檔案：專案結構與檔案用途
├── api-signature-protection-2026-09-21.plan.md # API 簽章保護 Lab 的完整執行計畫
├── docker-compose.yml                         # 本機 PostgreSQL 16 容器與 volume 設定
├── Taskfile.yml                               # task CLI 指令封裝（build/test/db-up/api-dev 等）
├── Lab.API.Signature.sln                      # .NET solution，包含 API 與測試專案
├── .issues/
│   └── worker2-plan-review-2026-09-21.md      # 計畫審核與設計補強紀錄
├── Lab.API.Signature/
│   ├── Lab.API.Signature.csproj               # net10.0 Web API 與 EF Core/Swagger 相依套件
│   ├── Program.cs                             # DI、Swagger、EF Core、Middleware 與 HTTP pipeline 設定
│   ├── Lab.API.Signature.http                 # API 呼叫範例檔
│   ├── appsettings.json                       # 共用 logging、host 與資料庫連線設定
│   ├── appsettings.Development.json           # Development 環境資料庫連線設定
│   ├── Properties/
│   │   └── launchSettings.json                # HTTP 5232 與 HTTPS 7011 啟動 profile
│   ├── Controllers/
│   │   └── OrdersController.cs                # GET/POST 受保護訂單示範端點
│   ├── Data/
│   │   └── SignatureDbContext.cs              # ApiKeyClient EF Core DbContext 與 demo seed
│   ├── Models/
│   │   └── ApiKeyClient.cs                     # 合作夥伴 ApiKey、Secret 與 metadata entity
│   ├── Repositories/
│   │   ├── IApiKeyClientRepository.cs          # 依 ApiKey 查詢憑證的 Repository 介面
│   │   └── ApiKeyClientRepository.cs           # EF Core Repository 實作
│   ├── Services/
│   │   ├── ISignatureValidationHandler.cs      # 簽章驗證服務介面
│   │   ├── SignatureValidationHandler.cs      # Canonical String、Timestamp、HMAC 驗證
│   │   ├── SignatureValidationModels.cs       # 驗證 request、failure reason 與 failure model
│   │   ├── INonceStore.cs                      # Nonce 原子消費服務介面
│   │   └── MemoryCacheNonceStore.cs            # IMemoryCache + per-key lock 防重放實作
│   ├── Middleware/
│   │   └── SignatureAuthenticationMiddleware.cs # protected path 的簽章驗證與 401 回應
│   ├── Migrations/
│   │   ├── 20260921134359_InitialCreate.cs    # InitialCreate Migration
│   │   ├── 20260921134359_InitialCreate.Designer.cs # Migration model metadata
│   │   └── SignatureDbContextModelSnapshot.cs # EF Core model snapshot
│   └── wwwroot/
│       ├── index.html                          # Client Demo 頁面與安全警告
│       ├── app.js                              # crypto.subtle 簽章與三種攻擊模擬
│       └── style.css                           # Client Demo 樣式
└── Lab.API.Signature.Tests/
    ├── Lab.API.Signature.Tests.csproj          # BDD、xUnit、WebApplicationFactory、Testcontainers 套件
    ├── Features/
    │   ├── SignatureProtection.feature        # 簽章保護 BDD 情境
    │   └── SignatureProtection.feature.cs     # Reqnroll 產生的 feature 程式碼
    ├── Services/
    │   ├── SignatureValidationHandlerTests.cs # 簽章核心邏輯單元測試（含 timestamp 邊界值）
    │   └── MemoryCacheNonceStoreTests.cs      # Nonce 與併發防重放單元測試
    ├── Middleware/
    │   └── SignatureAuthenticationMiddlewareTests.cs # 驗證便宜檢查先於讀 body、PayloadTooLarge 情境
    ├── Steps/
    │   └── SignatureProtectionSteps.cs        # BDD step definitions
    └── Support/
        ├── CustomWebApplicationFactory.cs     # 測試用 WebApplicationFactory
        ├── SignatureScenarioContext.cs        # BDD scenario 狀態
        ├── SignatureTestHelper.cs             # 測試簽章與 HTTP 請求輔助方法
        └── TestRunHooks.cs                    # Testcontainers 與測試生命週期 hooks
```
