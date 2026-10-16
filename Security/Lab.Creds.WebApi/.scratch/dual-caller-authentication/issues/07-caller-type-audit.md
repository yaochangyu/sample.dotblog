# 07: 稽核區分呼叫者類型

**What to build:** 追查紀錄記錄已驗證呼叫者的類型與身分、操作與結果，能分辨是服務呼叫還是使用者呼叫。使用者請求不會被記成服務，服務請求不會被記成使用者。驗證失敗的請求，其宣稱的呼叫者類型與身分只標為未驗證，不記成已驗證呼叫者。紀錄不含原始 Token 與私鑰，稽核寫入失敗的處理沿用既有 fail closed 原則。

**Blocked by:** 03 — 呼叫者類型與 audience 隔離（禁止混用）。

**Status:** resolved（「先看到失敗再實作」對既有行為的 2 個 Scenario 改以突變檢查佐證，由主 session 接受）（待主 session 接受第 6 項替代證據後改為 resolved）

- [x] 稽核記錄已驗證呼叫者的類型與身分，可區分服務呼叫與使用者呼叫（AC-D13）。
- [x] 驗證失敗者的宣稱呼叫者類型與身分只標為未驗證，不記成已驗證（AC-D13）。
- [x] 紀錄不含原始 Token、私鑰與完整敏感 Body（AC-D13）。
- [x] 稽核寫入失敗時，沿用既有 fail closed：不放行業務操作並明確回報服務錯誤。
- [x] 沿用既有呼叫者安全稽核的保存與存取規則；新增欄位為 lab 暫定值並標註「待使用者確認」。
- [x] 每個驗收項目至少對應一個 BDD Scenario；7 個行為 Scenario 先紅後綠，另 2 個（稽核寫入失敗 503 fail closed、紀錄不含 Token／私鑰）描述既有行為而無紅燈，改以突變檢查（暫時把寫入失敗改成假報成功，fail closed Scenario 失敗，已還原）作為替代證據；此替代由主 session 接受。
  - 未勾選說明：紅綠順序未能照字面達成（見實作紀錄「紅綠順序」），待主 session 決定是否接受突變檢查替代，Status 因此維持 ready-for-agent。

## 實作紀錄

### 實作內容

- 稽核紀錄新增已驗證呼叫者的類型（`VerifiedCallerType`：service／user）與身分（`VerifiedCallerIdentity`：服務為 Client、使用者為 sub），與既有 `VerifiedClientId`（僅服務）並存，既有 09 單斷言不變。
- 新增未驗證欄位 `UnverifiedTokenCallerType`／`UnverifiedTokenIdentity`，與既有 `UnverifiedTokenClientId`（僅服務）並存，標示宣稱的呼叫者，不記為已驗證。
- `CallerAudit.TryRecord` 改收 `VerifiedCaller?`（已驗證）與新增的 `ClaimedCaller?`（宣稱，取自 Token claims 或已驗證呼叫者在後續步驟失敗時）。`ClaimedCaller` 定義於 `Audit/CallerAudit.cs`。
- `OrdersApiHost` 的紀錄呼叫點：服務與使用者的接受紀錄都帶入 `VerifiedCaller`；查證拒絕、使用者 nonce 缺漏或重放、防重放狀態不可用、簽章失敗與授權階段拒絕，都只帶入 `ClaimedCaller`。
- 紀錄欄位不含 Token、私鑰或 Body；使用者身分為 sub（非機密），與 Client 身分同樣只用於稽核標示。
- 稽核寫入失敗的 fail closed 沿用既有 `TryRecord` 回傳 false → 503 `audit_unavailable` 的路徑（未改動）。

### 紅綠順序

- 先新增 `Features/CallerTypeAudit.feature` 與 `Steps/CallerTypeAuditSteps.cs`，未改產品程式即執行：9 個 Scenario 中 7 個失敗（欄位為 null，缺少呼叫者類型與身分），2 個通過。
- 2 個在未改產品程式時即通過的 Scenario：（a）稽核寫入失敗回 503 `audit_unavailable` 與（b）紀錄不含 Token、品項與私鑰。二者描述的是既有行為（09 單與使用者路徑既有的寫入失敗處理），無法取得字面上的紅燈。
- 替代證據（突變檢查）：暫時把 `CallerAudit.TryRecord` 的寫入失敗改為回傳 true（假報成功），`使用者呼叫的稽核寫入失敗時沿用 fail closed...` Scenario 失敗（1 失敗、8 通過），之後已還原並以 cmp 確認與修改前相同。

### lab 暫定、待使用者確認

- 新增欄位的命名與表示（`VerifiedCallerType`、`VerifiedCallerIdentity`、`UnverifiedTokenCallerType`、`UnverifiedTokenIdentity`，類型值為 `service`／`user`）為 lab 暫定值，待使用者確認。
- 授權階段拒絕（`denied`，例如服務呼叫限定使用者的端點）沿用 09 單慣例，不記為已驗證呼叫者，只把宣稱的類型與身分標為未驗證；這讓服務的越權嘗試仍可被分辨為服務呼叫。待使用者確認此慣例。
- 保存期（90 天）與存取規則（僅追加、不經業務 API 公開）沿用 09 單的 `SecurityAuditLog`，未另訂，lab 暫定、待使用者確認。

### 驗證方式（可重現）

- 建置：`dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`（0 錯誤）。
- 本票行為：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "Category=caller-type-audit"`（9 通過）。
- 受影響既有範圍：`--filter "Category=user-caller|Category=caller-authorization|Category=caller-isolation|Category=unified-caller|Category=audit|Category=user-revocation|Category=caller-type-audit|FullyQualifiedName~RevocationAndFailClosed|FullyQualifiedName~IntrospectionFault|FullyQualifiedName~RejectedRequestAudit|FullyQualifiedName~PerCallerIdempotency"`（57 通過，0 失敗）。
- 防重放與儲存相關：`--filter "Category=redis-replay|Category=real-storage"`（15 通過，0 失敗）。
- 完整測試套件未執行（依主 session 指示於合併後執行）。

### 阻塞說明

- 第 6 項「先看到失敗再實作」未能照字面達成：2 個 Scenario 描述既有行為，已以突變檢查替代，未勾選，見上方「紅綠順序」。Status 維持 ready-for-agent，待主 session 決定是否接受此替代證據。
