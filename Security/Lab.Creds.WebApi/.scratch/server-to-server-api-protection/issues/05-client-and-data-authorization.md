# 05: 隔離 Client 操作權限與業務資料範圍

**What to build:** 每個已驗證呼叫服務只能執行核准操作並讀取或修改自己的業務資料；其他合作廠商即使持有相同 scope 也不能越權。

**Blocked by:** 02 — 呼叫服務取得 Token 並經可信入口呼叫 API。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證，Gateway 不在本 lab 範圍）

- [x] 以兩個獨立 Client 與不同業務資料範圍展示允許和拒絕情境。
- [x] API／scope 白名單限制可執行操作；Token 有效但缺乏必要權限時拒絕。
- [x] 讀取與修改資料皆檢查業務資料範圍，不只在入口判斷 scope。
- [x] 相同 scope 的 Client 不能操作其他合作廠商的資料。
- [x] Body、查詢參數或路徑自行指定的組織／資料識別不能擴大已核准範圍。
- [x] 授權判斷依已驗證 Client 身分，不使用未驗證的身分資訊；拒絕的写入不產生副作用。
- [x] 授權資料來源與契約先確認；正向示範及越權整合情境可重現。
- [x] 此票可在受控測試環境先於簽章票驗證業務授權，不將未具完整保護的入口當成正式接入完成。

對應驗收：AC-12。

## 實作紀錄（05）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；Scenario 先寫並確認紅燈（9 failed）後才實作轉綠。

### 範圍與決策

- 授權資料來源：授權伺服器登錄每個 Client 的核准 scope 與 audience（`SpikeRuntime` 內記憶體登錄），Token 只含核准 scope；OpenIddict 伺服器登錄 scope 聯集，各 Client 只被授予自己的 scope 權限，要求未核准 scope 回應 `invalid_scope`。
- 契約：`poc/AuthSpike/doc/openapi.yml` 先更新（API First）：新增 `POST /orders/{orderId}/cancel`（修改訂單狀態，有 Body 的 POST，Body 為 `{}`），各操作新增 403（缺 scope），`GetOrderResponse` 新增 `status`。
- 兩個獨立 Client：`orders-client`、`orders-partner-client` 持有相同 scope（`orders.read orders.write`）但為不同合作廠商，各自只能操作自己的訂單；`billing-client` 只持有 `billing.write`，對建立訂單 API 仍由 audience 拒絕。
- 操作權限：有效 Token 缺少操作所需 scope（例如只有 `orders.read` 取消訂單）回應 403；policy 於業務 handler 與簽章驗證之前生效。
- 業務資料範圍：讀取與取消都在 handler 以已驗證 `client_id` 比對訂單擁有者；不屬於呼叫端的訂單回應 404，不洩漏存在與否。
- 不採信宣稱身分：Body、查詢參數（`?clientId=`）、路徑與 `X-Client-Id` 標頭宣稱的 clientId 一律忽略。
- 拒絕的寫入不產生副作用：403／404 的取消請求之後，擁有者查詢仍為 `active`。

### lab 暫定、待使用者確認

- scope 命名 `orders.read`、`orders.write`、`billing.write`（lab 暫定）。
- 跨廠商存取回應 404（lab 暫定，不區分「不存在」與「不屬於你」）。
- 取消訂單的路徑與 `POST` 方法（lab 暫定，以符合 03 單已核准的有 Body POST 簽章規則）。

### 尚未具備的保護（本階段明確範圍限制）

- 本票於受控測試環境驗證業務授權；不視為正式接入完成。
- 業務去重與 Idempotency Key 保存屬 06 單；nonce 防重放屬 04 單；撤銷與查證故障屬 07 單。
- Gateway 不在本 lab 範圍（本票無 Gateway 驗收項目）。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/ClientAndDataAuthorization.feature`

| 項目 | Scenario |
|---|---|
| 1 兩個獨立 Client 允許與拒絕 | `兩個獨立 Client 的允許與拒絕情境` |
| 2 scope 白名單與缺權限拒絕 | `未核准的 scope 要求不核發 Token`、`只有 orders.read 的有效 Token 不能取消訂單` |
| 3 讀取與修改檢查業務資料範圍 | `同 scope 的其他合作廠商不能取消他人訂單`、`同 scope 的其他合作廠商不能讀取他人訂單` |
| 4 相同 scope 不能操作他人資料 | `同 scope 的其他合作廠商不能取消他人訂單`、`同 scope 的其他合作廠商不能讀取他人訂單` |
| 5 Body／查詢參數／路徑不能擴大範圍 | `Body、查詢參數與路徑不能擴大已核准的資料範圍` |
| 6 已驗證身分與拒絕寫入無副作用 | `只有 orders.read 的有效 Token 不能取消訂單`、`授權依已驗證 Client 身分判斷，外部宣稱的身分標頭不影響授權且被拒絕的寫入不產生副作用` |
| 7 契約與示範可重現 | `授權資料來源、契約與正向及越權整合情境可重現` |
| 8 受控測試環境、非正式接入 | `本票為受控測試環境驗證，不視為正式接入完成` |

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```

### 未決事項與阻擋

- 無阻擋。上述 lab 暫定值已實作並標註，待使用者確認。
