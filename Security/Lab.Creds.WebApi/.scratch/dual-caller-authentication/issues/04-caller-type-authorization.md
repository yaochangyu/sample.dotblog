# 04: 授權依呼叫者類型分開

**What to build:** 授權規則依呼叫者類型分開定義：使用者只能操作自己的業務資料範圍；服務維持被核准的操作與業務資料範圍。服務的高權限不能用來代替使用者的權限限制（避免混淆代理人）。業務操作可以針對呼叫者類型設定允許或拒絕，寫入類或敏感操作可以只允許其中一種呼叫者。服務與使用者共用業務邏輯，但各自的 scope、角色與資料範圍語意不互相混用。

**Blocked by:** 03 — 呼叫者類型與 audience 隔離（禁止混用）。

**Status:** resolved

- [x] 使用者要求他人的業務資料時被資料範圍授權拒絕（AC-D07）。
- [x] 服務嘗試以自身高權限執行使用者專屬的限制操作時，依呼叫者類型授權規則被拒絕（AC-D08）。
- [x] 限定單一呼叫者類型的操作，由另一類型呼叫時被拒絕（AC-D14）。
- [x] 服務維持被核准的操作與業務資料範圍，不因新增使用者路徑而擴大。
- [x] 使用者的資料範圍與角色模型為 lab 暫定值並標註「待使用者確認」，不自行視為已核准。
- [x] 每個驗收項目至少對應一個 BDD Scenario，先看到失敗再實作。

## 實作紀錄

### 變更內容

- `poc/AuthSpike.Tests/Features/CallerTypeAuthorization.feature`（新增）：6 個行為 Scenario（`@caller-authorization @isolated`）與 1 個 `@record` 情境。涵蓋 AC-D07（他人的使用者訂單、服務建立的訂單、對照組）、AC-D08、AC-D14 與服務範圍不擴大；`@record` 情境檢查勾選、狀態與本實作紀錄。
- `poc/AuthSpike.Tests/Steps/CallerTypeAuthorizationSteps.cs`（新增）：自成一組步驟，步驟文字以「04 單」或「依呼叫者類型授權票」區分，不與其他 Steps 類別重複。
- `poc/AuthSpike/OrdersApi/OrdersApiHost.cs`：新增呼叫者類型授權政策 `user-only:orders.read`（要求呼叫者類型為使用者且持有 orders.read）、新增使用者專屬端點 `GET /orders/me/{orderId}`（路徑位於 `/orders` 前綴下，才會經過查證閘門）。服務呼叫者由政策以 403 拒絕，於查詢資料前即結束。
- `poc/AuthSpike/Verification/UserCallers.cs`：新增第二位測試使用者 `test-user-2`（`TestUsers` 表）與 `VerifyCredentials` 改為查表；既有 `test-user-1` 常數與行為不變。
- `poc/AuthSpike/AuthServer/AuthServerHost.cs`：密碼授權的 subject 與停用檢查改用請求的使用者帳號（原本寫死為 `test-user-1`）；測試使用者停用與無效帳密的既有行為不變。
- `poc/AuthSpike/doc/openapi.yml`：新增 `/orders/me/{orderId}` 契約（200／401／403／404）。

### 差距分析（實作前）

- 資料範圍：訂單以已驗證身分（使用者為 `sub`）為範圍，既有 `GET /orders/{id}` 對他人訂單回應 404，已符合 AC-D07 的資料範圍拒絕；本票以第二位測試使用者補上實際的跨使用者驗收。
- 呼叫者類型規則：既有 scope 政策只看權限，不看呼叫者類型，服務持有 orders.read 即可讀取任何端點；因此需要新增以呼叫者類型為條件的政策，並以使用者專屬端點驗證 AC-D08 與 AC-D14。
- 既有服務端點（`GET /orders/{id}`、`POST /orders`、`POST /orders/{id}/cancel`）未加上呼叫者類型限制，行為不變。

### TDD 順序與結果

- 紅燈：先寫 Feature（6 個行為 Scenario 與 `@record`）與步驟。篩選執行後 4 個情境失敗：AC-D08、AC-D14 與服務範圍不擴大因端點不存在而回應 404（預期 403）；AC-D07 跨使用者情境因第二位測試使用者無法取得 Token（invalid_grant）。另 2 個情境（服務建立的訂單被使用者查詢回應 404、使用者查詢自己的訂單回應 200）在基準上即通過，屬特徵化覆蓋（既有資料範圍已生效）。
- 綠燈：補上政策、端點、測試使用者表與密碼授權的使用者帳號後，6 個行為 Scenario 全數通過。
- `@record`：在本票勾選與狀態完成前為紅燈，完成後轉綠。

### 觀察與差距（未修正，記錄待後續）

- 使用者專屬端點 `GET /orders/me/{orderId}` 與既有 `GET /orders/{id}` 對使用者回應相同資料，目的是承載呼叫者類型規則的驗收，並非新的業務功能；是否保留或改為其他使用者專屬操作，待使用者確認。
- 呼叫者類型規則目前只驗證「服務拒絕使用者專屬操作」方向。反方向（使用者呼叫服務專屬操作）因 lab 尚無服務專屬操作而未覆蓋，留待需要時補上。
- 寫入類的呼叫者類型限制（例如只允許使用者取消）未納入本票：既有 `POST /orders/{id}/cancel` 同時允許服務與使用者，為避免改變既有服務行為而未調整；寫入類的使用者路徑防重放與冪等由 05 單處理。
- 使用者 Token 的 claim 與授權伺服器的 scope 白名單沿用 02 單，`orders.read`／`orders.write` 對使用者與服務共用同名 scope，但授權政策已分開以呼叫者類型判斷。

### lab 暫定、待使用者確認

- 使用者資料範圍為 lab 暫定值，待使用者確認：使用者只能讀取與取消 `sub` 相同身分建立的訂單；他人（其他使用者或服務）的訂單一律回應 404。
- 使用者角色模型為 lab 暫定值，待使用者確認：使用者與服務共用 `orders.read`／`orders.write` 兩個 scope 名稱，權限語意由呼叫者類型政策分開判斷；是否應為使用者另立 scope 名稱待使用者確認。
- 第二位測試使用者 `test-user-2` 與其密碼為 lab 測試資料，待使用者確認。
- 使用者專屬端點 `GET /orders/me/{orderId}` 的路徑與用途，待使用者確認。

### 驗證方式（可重現）

- `dotnet build poc/AuthSpike.Tests/AuthSpike.Tests.csproj`
- 只跑本票行為：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj --filter "DisplayName~AC-D07|DisplayName~AC-D08|DisplayName~AC-D14|DisplayName~服務維持被核准|DisplayName~04 單"`
- 完整：`dotnet test poc/AuthSpike.Tests/AuthSpike.Tests.csproj`（結果見下方修正紀錄與驗證結果）

### 修正紀錄與驗證結果

- 完整測試（於本票 worktree `impl/04-caller-type-authorization` 執行）：Total 385，通過 382，略過 2（既有 Gateway `@ignore`），失敗 1。
- 失敗項：「同一簽章併發送往單一執行個體只有一次通過」，為 server-to-server 04 單既有的併發情境，屬偶發失敗，與本票無關；本票未修改該情境，根因未定位。
- 單獨重跑該情境：3/3 通過。
- 本票 AC-D07、AC-D08、AC-D14 與服務範圍不擴大對應的 Scenario 於完整測試中皆通過。
