# 06: 使用者 Token 撤銷與查證故障 fail closed

**What to build:** 使用者 Token 被撤銷或使用者被停用後，後續請求在既有的撤銷上限內被拒絕，不繞過查證。使用者 Token 的查證服務故障且沒有符合時效的有效快取時，拒絕處理並明確回報服務錯誤，不誤判為 Token 無效，也不造成業務副作用，沿用既有 fail closed 原則。

**Blocked by:** 03 — 呼叫者類型與 audience 隔離（禁止混用）。

**Status:** resolved（除「先看到失敗再實作」改以突變檢查佐證外，驗收項目已達成）

- [x] 使用者 Token 被撤銷或使用者被停用後，既有撤銷上限內的後續請求被拒絕（AC-D11）。
- [x] 使用者 Token 查證服務故障且無有效快取時，拒絕處理並明確回報服務錯誤，無業務副作用（AC-D12）。
- [x] 故障回應與憑證或 Token 無效的回應可明確區分，不誤判為 Token 無效。
- [x] 使用者撤銷與查證故障不影響服務路徑既有的撤銷與 fail closed 行為。
- [x] 使用者 Token 的查證快取與撤銷同步機制為 lab 暫定值並標註「待使用者確認」。
- [x] 每個驗收項目至少對應一個 BDD Scenario，實測撤銷延遲在上限內；「先看到失敗再實作」未能照字面達成：行為已由 02 單的使用者路徑提供，新 Scenario 首次執行即通過，改以突變檢查（暫時把查證失敗改成被拒絕，兩個 AC-D12 Scenario 失敗，已還原）作為替代證據；此替代由主 session 接受。

## 實作紀錄

### 實作內容

- 本票未修改產品程式：使用者 Token 撤銷與使用者停用已由既有實作（02 單的使用者路徑、`TrustRegistry.IsUserEnabled` 每次請求直接讀取、`CallerVerifier` 的查證快取上限、`UserTokenIntrospector` 將連線失敗與非 200 回應一律轉為 `Unavailable`）滿足。本票新增 `Features/UserTokenRevocationAndFailClosed.feature` 與對應步驟 `UserTokenRevocationAndFailClosedSteps.cs`，從呼叫端直連建立訂單 API 驗證行為。
- 撤銷與停用：撤銷使用者 Token 後，於撤銷上限內（60 秒）以輪詢斷言首次回應 401；停用使用者後同樣於上限內拒絕；拒絕後的建立訂單請求 401 且無業務副作用（訂單數不增加）。
- 查證故障：使用者 Token 查證服務停止且無快取時回應 503 `verification_unavailable`，不為 401，無業務副作用；快取有效期間內的查詢仍回應 200 且未等待授權伺服器；快取到期後連續兩次查詢皆為 503（失敗不延長快取期限）。
- 服務路徑不受影響：使用者 Token 被撤銷後，服務 Token 的已簽章查詢仍回應 200。

### 紅綠順序的誠實紀錄

- 先寫 Scenario 再執行；五個行為 Scenario 在未修改產品程式下即全數通過，因此沒有觀察到「紅燈再轉綠」的實作循環。
- 為確認 AC-D12 的 Scenario 具鑑別力，暫時把使用者路徑的查證故障改為 Rejected（非 503）執行，兩個 AC-D12 Scenario 皆失敗；已還原該變更（工作樹僅含新增的 feature 與步驟檔）。
- 因此驗收文字中「先看到失敗再實作」一項未以紅燈達成，未勾選，見下方阻塞說明。

### lab 暫定、待使用者確認

以下皆為 lab 暫定、待使用者確認項目：

- 使用者 Token 的查證快取上限（沿用 `CallerVerifier` 的 lab 預設，預設 10 秒且不超過 Token 有效期）、撤銷同步機制（以授權伺服器內省結果為準、快取期間內撤銷可能延遲生效）為 lab 暫定值，待使用者確認。
- 撤銷與停用的 60 秒上限沿用 07 單既有的撤銷上限，套用到使用者路徑為 lab 暫定值，待使用者確認。
- 使用者停用的傳播方式為 lab 進程內登錄（`TrustRegistry`），跨程序或多執行個體的傳播未驗證，待使用者確認。

### 驗證方式（可重現）

- 建置：`dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`（0 錯誤）
- 本票行為：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "Category=user-revocation"`（5 通過）
- 受影響既有範圍：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "Category=user-caller|Category=caller-authorization|Category=caller-isolation|Category=unified-caller|Category=audit|Category=user-revocation|FullyQualifiedName~RevocationAndFailClosed|FullyQualifiedName~IntrospectionFault|FullyQualifiedName~RejectedRequestAudit|FullyQualifiedName~PerCallerIdempotency"`（48 通過，0 失敗）
- 完整測試套件由主 session 於合併後執行（本票未執行）。

### 阻塞說明

- 驗收第 6 項「每個驗收項目至少對應一個 BDD Scenario，實測撤銷延遲在上限內；先看到失敗再實作」：前半（Scenario 對應、撤銷延遲於上限內）已達成；後半「先看到失敗再實作」因既有實作已滿足而未以紅燈取得，故未勾選，Status 保持 ready-for-agent，待主 session 決定是否接受以故障注入驗證替代紅燈。
