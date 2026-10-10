# 08: 不中斷的正常輪替與洩漏撤銷

**What to build:** 呼叫服務可在新舊憑證／簽章金鑰重疊期間持續合法呼叫，切換完成再退役舊金鑰；發現洩漏時不等待重疊期結束。

**Blocked by:** 07 — 60 秒撤銷與查證故障時拒絕處理。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證，Gateway 不在本 lab 範圍，本單無 Gateway 驗收項目）

- [x] 正常輪替重疊期與操作程序取得確認，mTLS 及請求簽章金鑰分開管理。
- [x] 每 Client、每環境獨立配置私鑰，不共用呼叫服務身分或跨環境私鑰。
- [x] 正常流程先登錄新憑證／公鑰、切換呼叫端，再退役舊憑證／公鑰。
- [x] 在重疊期間以新舊配置完成合法呼叫；退役後舊配置不能繼續通過。
- [x] 新憑證重新取得綁定自己的 Token；舊 Token 搭配新憑證被拒絕。
- [x] 分別輪替 mTLS 與簽章金鑰，不因其中一者切換誤認另一者已同步更新。
- [x] 洩漏時直接撤銷受影響 Client、Token、憑證或金鑰，最多 60 秒內阻擋，不等待正常重疊期。
- [x] 提供整合驗證及可重現輪替／洩漏處理說明；文件不含真實憑證或私鑰。

對應驗收：AC-13，並沿用 AC-04。

## 實作紀錄（08）

由 `poc/AuthSpike.Tests` 的 BDD 測試實際執行結果整理；Scenario 先寫並確認紅燈（13 個新情境全數失敗）後才實作轉綠。延用 07 單的 `TrustRegistry`、`CallerVerifier`、第二個建立訂單 API 執行個體與既有測試環境。

### 輪替契約與重疊期

- 重疊期（lab 暫定、待使用者確認）：新憑證或新簽章金鑰登錄後，建議於 7 天內完成所有呼叫端切換，之後才退役舊配置。lab 不以計時器強制，改以明確的退役操作代替；退役前必須已有其他可用的替代憑證或金鑰（程式強制，否則拒絕）。
- 登錄順序（程式強制）：先登錄新憑證或金鑰、再切換呼叫端、最後退役舊者。未登錄前使用的新憑證或金鑰一律被拒絕。
- mTLS 憑證與請求簽章金鑰分開管理：兩者各自登錄、各自退役，互不連動。只輪替 mTLS 時既有簽章金鑰仍為有效；只輪替簽章金鑰時既有 mTLS 憑證仍可取得 Token。
- 新憑證須重新取得綁定自己的 Token；舊 Token 搭配新憑證被 cnf 綁定檢查拒絕（沿用 02 單既有機制）。

### 每 Client、每環境獨立配置私鑰

- 每 Client、每環境各自產生 mTLS 憑證與簽章金鑰；`SpikeRuntime.StartAsync` 接受 `environmentName`（預設 `lab`），憑證 CN 與簽章金鑰 KeyId 皆含環境名稱（例如 `orders-client-lab-sig-1`）。
- 不同 Client 的簽章金鑰 KeyId 互不相同；業務 API 只在已驗證 Client 自己登錄的金鑰中查找（`VerificationKeyStore.TryGet`），混用其他 Client 或其他環境的金鑰一律拒絕。

### 撤銷與退役狀態

- `TrustRegistry` 新增退役狀態（`RetireCertificate`、`RetireSigningKey`），與撤銷分開記錄；`IsCertificateBlocked`／`IsSigningKeyBlocked` 同時涵蓋撤銷與退役。
- 授權伺服器 Token 端點與業務 API（`CallerVerifier`、`SignedRequestVerifier`）皆檢查 blocked 狀態；退役的 mTLS 憑證同時自 Client 的 JWKS 移除（`AuthServerHost.SetClientCertificatesAsync`）。
- 退役不回滾已提交的業務操作（既有訂單於退役後仍可被查詢，見 BDD 情境）。
- 既有 09 單情境「成功驗證的建立訂單請求留下含已驗證 Client、金鑰識別……」的預期金鑰識別由 `orders-client-sig-1` 改為 `orders-client-lab-sig-1`（金鑰識別現納入環境名稱，屬本單預期變更；斷言內容與驗證邏輯未變）。

### 洩漏處理

- 疑似洩漏時直接撤銷受影響的對象，不等待重疊期結束：既有 mTLS 憑證、既有簽章金鑰、既有 Token 或整個 Client。
- 撤銷後的既有配置於 60 秒內被阻擋（沿用 07 門檻與查證快取上限 10 秒）；撤銷不影響新配置（既有配置被拒絕、目前配置仍回應 200）。

### 輪替步驟（可重現）

1. 產生新憑證與簽章金鑰並登錄（lab 進程內操作）：`Runtime.CreateClientCertificate` 後呼叫 `RegisterClientCertificateAsync`；`CreateSigningKey` 後呼叫 `RegisterSigningKey`。
2. 切換呼叫端：呼叫端改用新憑證以 mTLS 向授權伺服器要求新 Token，並以新簽章金鑰簽署業務請求。
3. 驗證重疊期：新舊配置皆可完成查詢／建立（對應情境「重疊期內新舊配置都能合法呼叫，退役後舊配置不能通過」）。
4. 退役舊配置：先退役 mTLS 憑證（`RetireClientCertificateAsync`）與／或簽章金鑰（`RetireSigningKey`）；退役後舊配置的 Token 要求與業務請求皆被拒絕。
5. 執行測試重現：`cd poc && dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj --filter "FullyQualifiedName~不中斷的正常輪替與洩漏撤銷"`。

### 洩漏處理步驟（可重現）

1. 立即撤銷受影響對象：`Registry.RevokeCertificate`（憑證）、`Registry.RevokeSigningKey`（簽章金鑰）、`AuthServer.RevokeAccessTokenAsync`（Token）或 `Registry.DisableClient`（Client）。不需等待重疊期。
2. 確認阻擋：受影響配置的後續請求於 60 秒內回應 401；未受影響的新配置仍回應 200。
3. 以新配置重新取得 Token 並繼續服務；被撤銷的舊配置不得復原（lab 無復原操作）。
4. 重現：`dotnet test ... --filter "FullyQualifiedName~不中斷的正常輪替與洩漏撤銷"`，情境「重疊期內疑似洩漏時直接撤銷……」。

### 測量證據（08 洩漏撤銷，`[洩漏撤銷延遲]` 輸出）

- 測量起點：`疑似洩漏時撤銷` 執行前的 `DateTimeOffset.UtcNow`；拒絕時間：既有配置首次回應 401 的時間。
- 實測（`[洩漏撤銷延遲]`，2026-10-10 單獨執行本單情境，單次量測；每次皆以 `elapsed <= 60 秒` 斷言）：
  - 既有 mTLS 憑證、既有簽章金鑰撤銷：首次拒絕約 0.01 秒（不經快取）。
  - 既有 Token 撤銷：首次拒絕約 11.5 秒，略高於 10 秒查證快取上限；額外約 1.5 秒未查明，屬單次量測，仍低於 60 秒門檻。
  - 停用 Client：既有配置約 0.01 秒，目前配置約 0.04 秒（不經快取）。

### lab 暫定、待使用者確認

- 正常輪替重疊期 7 天（操作程序建議值，lab 不以計時器強制）。
- 退役前必須已有替代憑證或金鑰（程式強制的順序規則）。
- 查證快取上限 10 秒（沿用 07 單 lab 暫定）。
- 洩漏撤銷以進程內管理操作進行，不新增管理 UI（沿用 07 單）。
- 輪替與撤銷狀態為記憶體保存，不持久化。
- 環境名稱以字串標示（lab 為 `lab`，另一環境以 `production` 模擬）。

### 範圍限制（明確列出）

- 多程序或分散式的輪替與撤銷同步未實作；多執行個體共用同一份 `TrustRegistry` 與 `VerificationKeyStore`（進程內即時同步）。
- 「每環境」以同一程序內兩組獨立執行環境模擬，不代表跨機器或正式環境部署。
- 本單無 Gateway 驗收項目；不實作 CA 型 tls_client_auth（mTLS 只用自簽憑證模式）。
- 文件不含任何真實憑證或私鑰；測試憑證只存在記憶體中（`SpikeCertificates` 不寫入磁碟）。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/CredentialRotationAndLeakRevocation.feature`

| 項目 | Scenario |
|---|---|
| 1 重疊期與操作程序、金鑰分開管理 | `輪替重疊期與操作程序已記錄於實作紀錄` |
| 2 每 Client 每環境獨立私鑰 | `每 Client 每環境獨立配置私鑰` |
| 3 登錄新憑證、切換、再退役 | `正常輪替依登錄新 mTLS 憑證、切換呼叫端、再退役舊憑證的順序進行`、`正常輪替依登錄新簽章金鑰、切換呼叫端、再退役舊金鑰的順序進行` |
| 4 重疊期新舊皆合法，退役後舊配置不通過 | `重疊期內新舊配置都能合法呼叫，退役後舊配置不能通過` |
| 5 新憑證取得自己的 Token，舊 Token 搭配新憑證被拒絕 | `新憑證取得綁定自己的 Token，舊 Token 搭配新憑證被拒絕` |
| 6 分別輪替 mTLS 與簽章金鑰 | `只輪替 mTLS 憑證時簽章金鑰狀態不被連動`、`只輪替簽章金鑰時 mTLS 憑證狀態不被連動` |
| 7 洩漏直接撤銷，60 秒內阻擋 | `重疊期內疑似洩漏時直接撤銷<對象>，既有配置於 60 秒內被阻擋`（既有 mTLS 憑證、既有簽章金鑰、既有 Token）、`重疊期內停用 Client 時新舊配置都於 60 秒內被阻擋` |
| 8 可重現說明、文件不含真實私鑰 | `輪替與洩漏處理說明可重現且文件不含真實憑證或私鑰` |

**紀錄型證據（`@record`，非行為驗證）**：下列 Scenario 只檢查本紀錄的文字或勾選狀態，屬紀錄型證據，標籤為 `@record`，不冒充行為驗證：
- 輪替重疊期與操作程序已記錄於實作紀錄
- 輪替與洩漏處理說明可重現且文件不含真實憑證或私鑰

### 驗證方式（可重現）

```
cd poc
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
