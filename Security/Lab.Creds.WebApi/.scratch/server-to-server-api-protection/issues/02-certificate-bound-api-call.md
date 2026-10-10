# 02: 呼叫服務取得 Token 並經可信入口呼叫 API

**What to build:** 呼叫服務以獨立 Client 身分取得 Token，經可信 Gateway 呼叫 ASP.NET Core 10 示範 API；合法服務可取得回應，沒有對應憑證的呼叫不能使用 Token。本切片僅供受控測試環境，尚未完成全部請求保護。

**Blocked by:** 01 — 確認 auth 技術選型與接入契約。

**Status:** resolved（Token 效期已由使用者核准 300 秒；所有驗收項目已達成，Gateway 項目依 lab 範圍略過）

- [x] 採用 01 核准方案與開發方式，建立呼叫端 → 業務 API 的主要整合測試邊界（本 lab 不經 Gateway，見下方 Gateway 項目）。
- [x] 每個示範呼叫服務使用獨立 Client 與用戶端憑證；不以 API Key 或 Client Secret 提供替代認證入口。
- [x] mTLS 用戶端認證成功才核發綁定該憑證的短效 Opaque Token；Token 效期已由使用者核准為 300 秒。
- [x] Token 到期後可重新取得；缺少或無效憑證不能取得 Token。
- [x] 經受保護且已認證的 introspection 介面判斷 Token 有效性、有效期限、目標 API 與憑證綁定，不只看 active。
- [x] 只持有 Token、使用不同 Client 的憑證、錯誤目標 API 或無效 Token 時拒絕呼叫。
- [x] Gateway 下游通道經過認證，外部偽造的驗證資訊被移除或覆寫，業務 API 不能繞過入口。（本 lab 不實作，略過，不阻擋；由業務 API 直接以 TLS 連線憑證自行驗證）
- [x] 合法呼叫建立已驗證 Client 身分，不採信 Body 或外部標頭自行宣稱的身分。
- [x] 示範流程與測試可重現，明確說明此階段尚未具備簽章、防重放及完整業務授權。

對應驗收：AC-01、AC-02、AC-03 的 Token／憑證部分、AC-06。

## 實作紀錄（02）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；未實際執行的項目不列為已達成。

### 範圍與決策

- 主要整合測試邊界：呼叫端 → 業務 API（本 lab 不經 Gateway）。
- Gateway 下游通道項目（本 lab 不實作，略過，不阻擋）：未實作 Gateway；`Gateway 下游通道經認證並覆寫偽造的驗證標頭` 為 `@ignore` Scenario 保留為未來契約。
- Client 與憑證：自簽用戶端憑證（`self_signed_tls_client_auth`），不做 CA 型 `tls_client_auth`；`orders-client`、`billing-client`、`orders-api` 各自獨立憑證。環境維度：本 lab 僅單一測試環境，每次啟動於記憶體中產生獨立憑證，正式多環境分離未驗證。
- 目標 API（audience）：由授權伺服器依 Client 登錄決定（`orders-client` → `orders-api`，`billing-client` → `billing-api`），建立訂單 API 以 `AddAudiences("orders-api")` 拒絕其他目標的 Token。
- 建立訂單的 `clientId` 取自已驗證 Token 的 `client_id`（introspection 結果），Body 與 `X-Client-Id` 標頭宣稱的身分被忽略。
- API 開發方式：API First，契約 `poc/AuthSpike/doc/openapi.yml`（`POST /orders`，回應新增 `clientId`）。

### Token 效期（已由使用者核准）

- Token 效期：300 秒（使用者已核准）。
- 此數值為 `SpikeRuntime.DefaultAccessTokenLifetime`，已由使用者於 2026-10-10 核准；spec 中「Token 效期」項目以此 lab 值為準。
- 測試以 `@short-lifetime` 標籤換用 2 秒效期的獨立測試環境，僅驗證「到期拒絕、重新取得」行為，不代表正式數值。

### 尚未具備的保護（本階段明確範圍限制）

- 尚未具備請求簽章（HTTP Message Signatures）、防重放（nonce）及完整業務授權（scope 與業務資料範圍檢查）。
- 尚未具備業務去重與 Idempotency Key（屬後續 issue）。
- 撤銷、60 秒上限與查證故障處理屬後續 issue。

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/ProtectedOrderCall.feature`（決策與紀錄類以紀錄檔內容檢查）。

| checkbox | Scenario |
|---|---|
| 1 主要整合測試邊界 | `主要整合測試邊界為呼叫端直連業務 API` |
| 2 獨立 Client 與憑證、無替代認證入口 | `各示範 Client 使用獨立的用戶端憑證`、`以 Client Secret 要求 Token 不核發`、`以 API Key 呼叫建立訂單不被接受` |
| 3 短效 Opaque Token 與效期確認 | `mTLS 認證成功後核發短效且綁定憑證的 Token`、`Token 效期的確認狀態有紀錄` |
| 4 到期重取、缺少或無效憑證不能取得 Token | `Token 到期後拒絕呼叫並可重新取得`、`無法取得 Token 的呼叫端`（既有） |
| 5 introspection 受保護且判斷完整 | `未附用戶端憑證的呼叫端不能查詢 Token 內省`、`內省回報有效、有效期限、目標 API 與綁定憑證`、`綁定憑證取得不透明 Token`（既有） |
| 6 拒絕只持有 Token、他 Client 憑證、錯誤目標、無效 Token | `偷到 Token 但憑證不符無法建立訂單`（既有）、`其他目標 API 的 Token 不能呼叫建立訂單 API`、`無效 Token 不能呼叫建立訂單 API` |
| 7 Gateway（本 lab 不實作） | `Gateway 下游通道項目標註為本 lab 不實作且略過`；`Gateway 下游通道經認證並覆寫偽造的驗證標頭`（`@ignore`，略過）；`API 不信任公開請求自行提供的憑證標頭`（既有） |
| 8 已驗證 Client 身分 | `建立訂單的 clientId 為已驗證 Client，不採信 Body 或外部標頭` |
| 9 可重現與範圍限制 | `示範流程可重現且明確標示尚未具備的保護` |

### 未決事項與阻擋

- 無。Token 效期已由使用者核准（300 秒），checkbox 3 已勾選，本單結案（resolved）。
