# 14: 管理員退役與撤銷憑證與簽章金鑰

**What to build:** 只有已驗證的管理員能退役或撤銷 Client 的憑證與簽章金鑰。退役沿用既有的輪替重疊規則，撤銷沿用既有的 60 秒上限，管理操作本身不放寬這些限制。非管理員（包括該 Client 自己）不能退役或撤銷信任名單的項目。管理員透過管理介面操作後，Client 的後續呼叫依既有行為被拒絕。

**Blocked by:** 12 — 憑證登錄申請與核准／拒絕；13 — 簽章金鑰登錄申請與核准；07 — 60 秒撤銷與查證故障時拒絕處理；08 — 不中斷的正常輪替與洩漏撤銷。

**Status:** resolved（所有驗收項目已達成；受控測試環境驗證；退役與撤銷端點與回應格式為 lab 暫定值，待使用者確認；本單無 Gateway 驗收項目；管理操作稽核屬 15 單，未實作）

- [x] 管理員退役憑證或簽章金鑰後，依既有輪替規則生效，重疊期內的新舊項目行為符合 08 單契約（AC-19）。
- [x] 管理員撤銷憑證或簽章金鑰後，各驗證端含既有連線上的後續請求，最多 60 秒內阻擋（AC-19）。
- [x] 非管理員（無管理憑證或持 Client 憑證）嘗試退役或撤銷時被拒絕，信任名單不變（AC-15、AC-19）。
- [x] 管理操作退役或撤銷不使其他 Client 的憑證、金鑰或權限受影響（AC-23）。
- [x] 撤銷對象不存在或已撤銷時回應明確且不造成錯誤狀態。
- [x] 每個驗收項目至少對應一個 BDD Scenario，實測撤銷延遲小於 60 秒；沿用既有撤銷與 fail closed 行為。

## 實作紀錄（14）

Feature 檔 `poc/AuthSpike.Tests/Features/AdminRetireAndRevoke.feature` 先寫並確認紅燈（15 個 Scenario 全數失敗，原因為管理端點不存在而回應 404），實作後行為 Scenario 全綠，最後勾選驗收。步驟檔 `Steps/AdminRetireAndRevokeSteps.cs` 全部經真實 mTLS 與 HTTP 路徑呼叫，不直接呼叫授權伺服器的內部登錄方法。延用既有 `TrustRegistry`、`VerificationKeyStore`、`AuthServerHost` 的組合根模式。

### 端點（lab 暫定、待使用者確認）

- `POST /admin/clients/{clientId}/certificates/{thumbprint}/retire`、`.../revoke`：退役或撤銷 Client 的 mTLS 憑證。
- `POST /admin/clients/{clientId}/signing-keys/{keyId}/retire`、`.../revoke`：退役或撤銷 Client 的請求簽章金鑰。
- 成功回應 200，Body 含 `clientId`、對象識別與 `status`（`retired` 或 `revoked`）。
- 錯誤回應：401 `administrator_authentication_required`（非管理員，先於對象存在性檢查）；404 `client_not_found`、`certificate_not_found`、`signing_key_not_found`；409 `replacement_required`（退役時沒有其他可用替代項目）、`already_revoked`、`already_retired`（不改變任何狀態）、`certificate_shared`、`signing_key_shared`（同一指紋或 keyId 也登錄於其他 Client，拒絕以免波及其他 Client）。

### 規則與決策

- **狀態權威仍為 `TrustRegistry`**：Token 端點與業務 API 每次請求都讀取登錄狀態（不經快取），因此既有連線上的後續請求即時被阻擋，遠低於 60 秒上限。撤銷延遲由 `既有連線的建立訂單請求在 60 秒內被阻擋` 步驟實測（以既有連線輪詢直到回應 401，斷言延遲小於 60 秒）。
- **退役沿用輪替重疊規則**：退役憑證或簽章金鑰必須已有其他未退役、未撤銷的替代項目；撤銷不要求替代項目，以免洩漏時無法撤銷。
- **OpenIddict JWKS 不移除已退役或已撤銷的憑證**：實測移除最後一張憑證會使 OpenIddict 的 `UpdateAsync` 拋出驗證例外（confidential Client 採 self_signed_tls_client_auth 時，JWKS 不可為空），回應 500。改為只由 `TrustRegistry` 擋下：Token 端點既有的 `IsCertificateBlocked` 檢查會拒絕已退役或已撤銷的憑證。信任名單快照改為只列出未退役、未撤銷的憑證。
- **同一指紋或 keyId 不跨 Client**：`TrustRegistry` 以指紋與 keyId 為全域鍵，因此管理操作在 Client 不符時一律拒絕（`certificate_shared`、`signing_key_shared`），確保 AC-23。
- **管理操作序列化**：退役與撤銷與核准、拒絕共用同一決策閘門。

### 驗收 checkbox 對應 Scenario

檔案：`poc/AuthSpike.Tests/Features/AdminRetireAndRevoke.feature`

| 項目 | Scenario |
|---|---|
| 1 退役依輪替規則生效（AC-19） | `重疊期內新舊憑證皆可取得 Token，管理員退役舊憑證後舊憑證被拒絕且新憑證仍可用`、`重疊期內新舊簽章金鑰皆可通過驗證，管理員退役舊金鑰後舊金鑰簽署的請求被拒絕`、`沒有替代項目時退役被拒絕，信任名單與簽章金鑰狀態不變` |
| 2 撤銷後既有連線在 60 秒內阻擋（AC-19） | `撤銷憑證後既有連線上的請求在 60 秒內被阻擋`、`撤銷唯一憑證後既有連線在 60 秒內被阻擋且不再核發 Token`、`撤銷簽章金鑰後既有連線上的請求在 60 秒內被阻擋，憑證不受影響` |
| 3 非管理員被拒絕（AC-15、AC-19） | `非管理員不能退役或撤銷 Client 的憑證，信任名單不變`（Scenario Outline：無憑證、未登錄憑證、Client 憑證）、`非管理員不能退役或撤銷 Client 的簽章金鑰，既有金鑰仍可通過驗證`（Scenario Outline） |
| 4 不影響其他 Client（AC-23） | `退役與撤銷 orders-client 的項目不影響其他 Client 的憑證、金鑰與權限` |
| 5 不存在或已撤銷時回應明確 | `撤銷不存在或已撤銷的對象回應明確且狀態不變` |
| 6 BDD 覆蓋與實測延遲 | 上列各 Scenario 皆經真實 TLS 與 HTTP 路徑；`14 單的實作紀錄已標註退役與撤銷端點為 lab 暫定值`（`@record`） |

**紀錄型證據（`@record`，非行為驗證）**：`14 單的實作紀錄已標註退役與撤銷端點為 lab 暫定值` 只檢查本紀錄的文字與勾選狀態，不證明系統行為。

### 待查與範圍限制

- 待查：`TrustRegistry` 以指紋與 keyId 為全域鍵，目前以管理操作的歸屬檢查避免波及其他 Client；若要從根本分離，需改為 clientId 加 keyId 的複合鍵，會改動既有測試的呼叫點，本單未做。
- 待查：13 單的 `SpikeRuntime.RegisterSigningKey` 與 `RetireSigningKey`（08 單程序內方法）仍不經管理員核准與退役管理端點，本單未改動。
- 本單不實作管理操作稽核（15 單）；退役與撤銷目前不寫入稽核紀錄。
- 本單未更新 `doc/openapi.yml`（ADR 0001 定位為契約文件）。
- 未實作 Gateway；Gateway 相關項目不在本單範圍。

### 驗證方式（可重現）

```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj --filter "FullyQualifiedName~管理員退役與撤銷憑證與簽章金鑰"
```
