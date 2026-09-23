# Lab.Signature.WebApi 專案結構

以下結構排除 `bin/`、`obj/`、`.git/` 與其他執行產物，保留原始碼、測試、Migration、設定與文件。
主專案與測試專案放在 `src/` 底下，其餘 solution 層級檔案（`.sln`、`Taskfile.yml`、文件等）留在專案根目錄。

```text
Lab.Signature.WebApi/
├── README.md                                  # 專案介紹、架構、簽章規格、啟動與風險說明
├── tree.md                                    # 本檔案：專案結構與檔案用途
├── doc/
│   ├── key-application-form-example.md        # API Key 人工申請單的空白表格與填寫範例
│   ├── key-application-process.md             # API Key 申請、審核、核發與安全交付流程
│   ├── signature-generation-and-verification.md # Client 產生與 Server 驗證 HMAC 簽章
│   └── protected-api-verification.md          # 受保護 API 請求的完整驗證流程
├── api-signature-protection-2026-09-21.plan.md # API 簽章保護 Lab 的完整執行計畫
├── docker-compose.yml                         # 本機 PostgreSQL 16 容器與 volume 設定
├── Taskfile.yml                               # task CLI 指令封裝（build/test/db-up/api-dev 等，API_DIR 指向 src/Lab.Signature.WebApi）
├── Lab.Signature.WebApi.sln                   # .NET solution，專案路徑指向 src/ 底下的兩個專案
├── .archive/
│   ├── admin-key-issuance-2026-09-23.plan.md  # Admin API Key 頒發機制的完整執行計畫（已完成封存）
│   └── rename-to-lab-signature-webapi-2026-09-23.plan.md # 專案改名計畫（已完成封存）
├── .issues/
│   ├── worker2-plan-review-2026-09-21.md      # 計畫審核與設計補強紀錄
│   ├── worker2-admin-key-review-2026-09-23.md # Admin Key 功能審查與修正紀錄
│   └── worker2-rename-review-2026-09-23.md    # 專案改名審查紀錄
└── src/
    ├── Lab.Signature.WebApi/
    │   ├── Lab.Signature.WebApi.csproj            # net10.0 Web API 與 EF Core/Swagger 相依套件
    │   ├── Program.cs                             # DI、Swagger、EF Core、Middleware 與 HTTP pipeline 設定
    │   ├── Lab.Signature.WebApi.http               # API 呼叫範例檔
    │   ├── appsettings.json                       # 共用 logging、host 與資料庫連線設定
    │   ├── appsettings.Development.json           # Development 環境資料庫連線設定
    │   ├── Properties/
    │   │   └── launchSettings.json                # HTTP 5232 與 HTTPS 7011 啟動 profile
    │   ├── Controllers/
    │   │   ├── AdminClientsController.cs          # Admin API 核發與查詢 Client
    │   │   └── OrdersController.cs                # GET/POST 受保護訂單示範端點
    │   ├── Data/
    │   │   └── SignatureDbContext.cs              # ApiKeyClient EF Core DbContext 與 demo seed
    │   ├── Models/
    │   │   └── ApiKeyClient.cs                     # 合作夥伴 ApiKey、Secret 與 metadata entity
    │   ├── Repositories/
    │   │   ├── IApiKeyClientRepository.cs          # 依 ApiKey 查詢憑證的 Repository 介面
    │   │   └── ApiKeyClientRepository.cs           # EF Core Repository 實作
    │   ├── Services/
    │   │   ├── IApiKeyGenerator.cs                # ApiKey/Secret 產生器介面
    │   │   ├── ApiKeyGenerator.cs                 # 使用密碼學安全亂數產生 ApiKey/Secret
    │   │   ├── ISignatureValidationHandler.cs      # 簽章驗證服務介面
    │   │   ├── SignatureValidationHandler.cs      # Canonical String、Timestamp、HMAC 驗證
    │   │   ├── SignatureValidationModels.cs       # 驗證 request、failure reason 與 failure model
    │   │   ├── INonceStore.cs                      # Nonce 原子消費服務介面
    │   │   └── MemoryCacheNonceStore.cs            # IMemoryCache + per-key lock 防重放實作
    │   ├── Middleware/
    │   │   ├── AdminAuthenticationMiddleware.cs    # Admin path 的環境與 X-Admin-Key 驗證
    │   │   └── SignatureAuthenticationMiddleware.cs # protected path 的簽章驗證與 401 回應
    │   ├── Migrations/
    │   │   ├── 20260921134359_InitialCreate.cs    # InitialCreate Migration
    │   │   ├── 20260921134359_InitialCreate.Designer.cs # Migration model metadata
    │   │   └── SignatureDbContextModelSnapshot.cs # EF Core model snapshot
    │   └── wwwroot/
    │       ├── index.html                          # Client Demo 頁面與安全警告
    │       ├── app.js                              # crypto.subtle 簽章與三種攻擊模擬
    │       └── style.css                           # Client Demo 樣式
    └── Lab.Signature.WebApi.Tests/
        ├── Lab.Signature.WebApi.Tests.csproj       # BDD、xUnit、WebApplicationFactory、Testcontainers 套件（ProjectReference 相對路徑指向同層的 Lab.Signature.WebApi）
        ├── Features/
        │   ├── SignatureProtection.feature        # 簽章保護 BDD 情境
        │   ├── SignatureProtection.feature.cs     # Reqnroll 產生的 feature 程式碼
        │   ├── AdminKeyIssuance.feature           # Admin API 核發、清單與環境驗證情境
        │   └── AdminKeyIssuance.feature.cs        # Reqnroll 產生的 Admin feature 程式碼
        ├── Repositories/
        │   └── ApiKeyClientRepositoryTests.cs     # ApiKeyClient Repository 新增、查詢與衝突測試
        ├── Services/
        │   ├── ApiKeyGeneratorTests.cs            # ApiKey/Secret 格式與不重複測試
        │   ├── SignatureValidationHandlerTests.cs # 簽章核心邏輯單元測試（含 timestamp 邊界值）
        │   └── MemoryCacheNonceStoreTests.cs      # Nonce 與併發防重放單元測試
        ├── Middleware/
        │   ├── AdminAuthenticationMiddlewareTests.cs # Admin Key 與非 Development 404 測試
        │   └── SignatureAuthenticationMiddlewareTests.cs # 驗證便宜檢查先於讀 body、PayloadTooLarge 情境
        ├── Steps/
        │   ├── AdminKeyIssuanceSteps.cs            # Admin API BDD step definitions
        │   └── SignatureProtectionSteps.cs        # BDD step definitions
        └── Support/
            ├── AdminApiTestHelper.cs              # Admin API 測試 Header 與設定值輔助方法
            ├── AdminScenarioContext.cs            # Admin API BDD scenario 狀態
            ├── CustomWebApplicationFactory.cs     # 測試用 WebApplicationFactory
            ├── SignatureScenarioContext.cs        # BDD scenario 狀態
            ├── SignatureTestHelper.cs             # 測試簽章與 HTTP 請求輔助方法
            └── TestRunHooks.cs                    # Testcontainers 與測試生命週期 hooks
```
