# 03: 業務 API 驗證原始呼叫端請求簽章

**What to build:** 呼叫服務簽署示範業務請求，最終 API 能辨認原始呼叫端並拒絕被竄改或混用身分的請求，合法簽章請求維持正常回應。

**Blocked by:** 02 — 呼叫服務取得 Token 並經可信入口呼叫 API。

**Status:** resolved（共同簽章規則已由使用者核准；所有驗收項目已達成，Gateway 項目依 lab 範圍略過）

- [x] 取得共同 HTTP Message Signatures 規則與演算法的確認，明訂必要欄位、Token 綁定資訊表示及中介改寫處理。
- [x] 所有示範業務 API 呼叫必須簽章；無 Body 請求仍驗證方法、目標與必要欄位。
- [x] 有 Body 時簽章涵蓋摘要與必要內容型別，最終 API 核對摘要和收到的實際 Body 相符。
- [x] 涵蓋完整業務目標、會影響語意的查詢參數、必要標頭、適用時的 Idempotency Key 及 Token 綁定資訊。
- [x] 簽章使用不同於 mTLS 的私鑰，並確認簽章金鑰屬於已驗證 Token 識別的同一 Client。
- [x] 缺少簽章、內容或目標遭竄改、替換授權脈絡、混用不同 Client 合法金鑰時拒絕。
- [x] Gateway 不破壞簽章驗證脈絡；若有改寫，以已確認契約保留可驗證的原始資訊，不信任外部自稱的原始欄位。
- [x] 使用實際簽署與驗證的整合情境證明合法請求可通過及竄改請求被拒絕，不宣稱 nonce 防重放已完成。

對應驗收：AC-03 的簽章金鑰部分、AC-07。

## 實作紀錄（03）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；未實際執行的項目不列為已達成。

### 範圍與決策

- 業務 API 採 API First：`poc/AuthSpike/doc/openapi.yml` 新增 `GET /orders/{orderId}`（無 Body），並於描述說明簽章要求。
- 簽章規則（使用者已核准提案值，第 1 項已勾選）：
  - 演算法：`ecdsa-p256-sha256`（RFC 9421），簽章金鑰為每個 Client 獨立的 ECDSA P-256 金鑰，與 mTLS 憑證金鑰分開。
  - 有 Body（POST）必要元件：`@method`、`@target-uri`、`@query`、`authorization`、`content-type`、`content-digest`、`idempotency-key`。
  - 無 Body（GET）必要元件：`@method`、`@target-uri`、`@query`、`authorization`。
  - 摘要：RFC 9530 `Content-Digest`（sha-256），API 另以實際 Body 重算核對。
  - 參數：`created`、`expires`、`keyid`、`alg`、`nonce`；接受窗為 created 不超前 30 秒、expires 60 秒（自簽發起算），且未超過 expires（使用者已核准）。
  - Token 綁定：簽章涵蓋 `authorization` 標頭，替換 Token 即簽章失效。
  - 金鑰歸屬：`keyid` 必須為已驗證 Token 的 `client_id` 所登錄的金鑰，否則拒絕。
- 驗證位置：業務 API 於 Token 驗證（UseAuthorization）之後以中介層驗證簽章；未實作 Gateway（第 7 項略過）。
- 呼叫端簽章與業務 API 驗證共用 `poc/AuthSpike/Signing/HttpMessageSignature.cs`。

### 未完成與明確範圍限制

- nonce 僅被簽署，未檢查是否重複；未完成防重放，屬 04 單。
- 未實作業務去重與 Idempotency Key 保存，屬 06 單；`Idempotency-Key` 目前只要求簽署並必填。
- 查詢訂單僅以 Client 擁有權回傳 200 或 404；完整業務資料範圍檢查屬 05 單。
- 02 的既有測試改為以 Token 所屬 Client（orders-client）的簽章金鑰簽署，負向案例仍只由 02 的因素決定拒絕。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/SignedBusinessRequests.feature`

| 項目 | Scenario |
|---|---|
| 1 共同簽章規則（已核准） | `共同簽章規則已由使用者核准並記錄` |
| 2 所有業務呼叫簽章、無 Body 驗證方法與目標 | `已簽章的建立訂單請求通過`、`已簽章的查詢訂單請求通過（無 Body）`、`未簽章的建立訂單請求被拒絕`、`未簽章的查詢訂單請求被拒絕`、`無 Body 的查詢簽章改動目標被拒絕` |
| 3 有 Body 涵蓋摘要與內容型別，API 核對實際 Body | `有 Body 時簽章涵蓋內容摘要與內容型別`、`改動已簽署的 Body 被拒絕`（Scenario Outline） |
| 4 完整目標、查詢參數、必要標頭、Idempotency Key、Token 綁定 | `已簽章的查詢參數被改動被拒絕`、`改動已簽署的 目標／Idempotency-Key 標頭／Content-Type 標頭／授權 Token 被拒絕`（Scenario Outline） |
| 5 簽章金鑰與 mTLS 私鑰分開，且屬於已驗證 Client | `簽章金鑰與 mTLS 用戶端憑證私鑰分開`、`持有 orders-client Token 但以 billing-client 簽章金鑰簽署被拒絕` |
| 6 缺少簽章、竄改、替換授權脈絡、混用金鑰拒絕 | `缺少必要簽章標頭被拒絕`（Scenario Outline）、`已逾期的簽章被拒絕`、以及第 4、5 項的負向 Scenario |
| 7 Gateway（本 lab 不實作，略過） | `Gateway 項目標註為本 lab 不實作且略過` |
| 8 實際簽署與驗證的整合情境，不宣稱 nonce 防重放 | `實際簽署與驗證的整合情境區分合法與竄改請求` |

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

### 未決事項與阻擋

- 無未決事項。第 1 項已由使用者核准：`ecdsa-p256-sha256`、既有涵蓋欄位、`created` 不超前 30 秒、`expires` 60 秒。
- nonce 防重放與業務去重仍分別屬 04、06 單，不在本單結案範圍。
