# Server-to-Server API 保護規格

Status: ready-for-agent

日期：2026-10-07  
狀態：通用設計決策已確認；尚未實作，實作參數待具體 API 設計。

## Problem Statement

內部服務與外部合作廠商需要以服務自身身分安全呼叫 API。單純 API Key 或 Client Credentials 無法同時滿足服務權限隔離、憑證撤銷與輪替、呼叫追查、Token 防冒用、業務去重與請求內容簽章等需求。

呼叫端因逾時重試、攻擊者重放請求，或憑證洩漏時，API 必須維持明確的接受判斷與本地業務提交保證，不能以「Token 有效」取代所有檢查。

## Solution

以集中式 OAuth 2.0 Client Credentials 建立服務身分與授權，使用 mTLS 用戶端認證及憑證綁定 Opaque Token，使 Token 無法被單獨冒用。所有業務 API 採 HTTP Message Signatures，由最終 API 驗證原始呼叫端簽章與防重放資訊。

各 Client 的 API／scope 與業務資料範圍分別檢查；有副作用的操作以穩定業務識別及 Idempotency Key 保證本地提交不重複。集中查證及撤銷流程最多 60 秒內阻擋後續使用，無法取得符合時效的驗證結果時拒絕處理。

## User Stories

1. 身為內部服務維護者，我希望服務使用獨立 Client 身分，以便權限與追查紀錄不與其他服務混用。
2. 身為外部合作廠商，我希望使用明確的 server-to-server 接入契約，以便不需要互動式使用者登入也能取得授權。
3. 身為平台管理者，我希望在集中式授權伺服器管理 Client，以便統一核發與撤銷授權。
4. 身為呼叫服務維護者，我希望以 mTLS 取得 Token，以便不另外管理 Client Secret。
5. 身為 API 維護者，我希望 Access Token 綁定呼叫端憑證，以便偷到 Token 的人不能單憑 Token 呼叫 API。
6. 身為呼叫服務維護者，我希望取得短效 Token 並於到期後重新取得，以便不依賴長期 Token。
7. 身為平台管理者，我希望撤銷服務、Token、憑證或簽章金鑰後最多 60 秒內阻擋後續使用，以便限制洩漏影響。
8. 身為 API 維護者，我希望查證服務故障且沒有時效內的有效快取時拒絕處理，以便不破壞撤銷保證。
9. 身為呼叫服務維護者，我希望暫時無法查證時收到明確服務錯誤，以便不把系統故障誤認為憑證已失效。
10. 身為平台管理者，我希望 API Key 不再承擔認證或授權責任，以便不維護第二套可繞過主要保護的認證入口。
11. 身為平台管理者，我希望有需求時才保留獨立訂閱計量 Key，以便將計量與安全授權分開。
12. 身為 API 維護者，我希望可信 Gateway 可以終止 mTLS，以便支援集中式入口而不失去憑證綁定保證。
13. 身為 API 維護者，我希望阻擋偽造 Gateway 身分資訊及繞過入口的請求，以便不信任呼叫端自行宣稱的驗證結果。
14. 身為資料擁有者，我希望每個 Client 只能呼叫核准的 API 與操作，以便落實服務權限隔離。
15. 身為合作廠商，我希望其他廠商即使有相同 scope 也不能操作我的資料，以便維持資料範圍隔離。
16. 身為 API 維護者，我希望所有業務呼叫都具備標準請求簽章，以便驗證原始呼叫者及被簽署內容。
17. 身為 API 維護者，我希望簽章金鑰屬於 Token 識別的同一 Client，以便拒絕混用不同服務的合法憑證。
18. 身為資料擁有者，我希望 API 核對收到的 Body 與已簽署摘要一致，以便內容遭竄改時被拒絕。
19. 身為 API 維護者，我希望目標、必要標頭及授權脈絡受到簽章保護，以便請求不能被改送或替換語意。
20. 身為 API 維護者，我希望跨執行個體阻擋同一 nonce 的併發重放，以便擴充部署不削弱防重放。
21. 身為呼叫服務維護者，我希望重試時使用新 nonce 與簽章、沿用業務識別及 Idempotency Key，以便合法重試不重做業務副作用。
22. 身為業務操作發起者，我希望相同操作及內容的重試提供既有結果或處理狀態，以便逾時不導致重複提交。
23. 身為業務操作發起者，我希望相同識別卻不同內容時明確被拒絕，以便既有操作不被覆寫。
24. 身為業務系統維護者，我希望更換 Idempotency Key 仍不能重做同一業務操作，以便去重不只依賴呼叫端的請求 Key。
25. 身為業務系統維護者，我希望短期去重紀錄過期後仍維持業務唯一性，以便保存期結束不造成重複副作用。
26. 身為業務系統維護者，我希望併發、當機與提交後回應遺失都不造成重複本地提交，以便重試具備可靠的業務語意。
27. 身為服務維護者，我希望 mTLS 與簽章金鑰分開且各環境獨立，以便用途、輪替與撤銷互不混淆。
28. 身為服務維護者，我希望正常輪替允許新舊金鑰短暫重疊，以便完成切換再退役舊金鑰。
29. 身為安全營運人員，我希望洩漏時直接撤銷受影響的身分或金鑰，以便不必等待正常輪替重疊期。
30. 身為稽核人員，我希望紀錄已驗證 Client、金鑰識別、操作與結果，以便追查實際呼叫者。
31. 身為安全營運人員，我希望未驗證身分與已驗證身分明確區分，以便攻擊者不能偽造可信追查紀錄。
32. 身為資料擁有者，我希望追查紀錄不包含原始 Token、私鑰或完整敏感 Body，以便紀錄不成為洩漏來源。
33. 身為下游整合維護者，我希望本地提交保證與跨系統冪等責任清楚分開，以便不誤認為已具備跨系統 exactly-once。

## Implementation Decisions

### 實作平台與開發規則

- 業務 API 採 ASP.NET Core 10（.NET 10）。
- 實作遵循 [api.template 開發規則](https://github.com/yaochangyu/api.template/blob/main/CLAUDE.md)，並依其導航閱讀必讀文件與工作項指南。
- 全專案確認採 API First，定義最小 OpenAPI 規格，不做 codegen。
- OAuth 授權核心：使用者已確認採用 OpenIddict 7.7.1 作為集中式授權伺服器核心。
- 資料存取採 EF Core + PostgreSQL，測試環境採隔離之 Testcontainers，規格驗收採 Reqnroll BDD（程式碼生成於 `obj/`，不追蹤 `.feature.cs`）。
- Gateway 與 Token 接入：使用者已確認 Lab 實作基線採用 Envoy v1.39.3（`envoyproxy/envoy:v1.39.3@sha256:dd85940439de19a0b6ae8419610363ea0ad351d9a994ea007161c206ec1e1865`）作業務 API Gateway；Token 端點則由呼叫端（caller）直接以 mTLS 連線授權伺服器（不經 Gateway）。此為 Lab 選型，不等於 production 驗收。

### 已確認的選型

| 面向 | 決策 |
|---|---|
| 授權流程 | OAuth 2.0 Client Credentials，集中式授權伺服器（核心採用 OpenIddict 7.7.1） |
| Token 端點接入路徑 | 呼叫端（caller）直接以 mTLS 連線授權伺服器（不經 Gateway） |
| Token 端點的用戶端認證 | mTLS，Lab 第一版暫以已驗證 RSA-2048 自簽 client cert 接入，不新增 Client Secret；其他憑證類型保留未驗 |
| Access Token | Opaque Token，綁定呼叫端憑證 |
| Token 有效性 | 集中 introspection；允許符合撤銷上限的快取 |
| 業務 API Gateway（Lab 基線） | Envoy v1.39.3 終止業務 API mTLS（Lab 選型，非 production 驗收）；Envoy→API 獨立 mTLS 且 API 固定 Gateway 憑證指紋防繞過 |
| 憑證轉送與覆寫 | Envoy 以 `forward_client_cert_details: SANITIZE_SET` 覆寫 XFCC；API 僅在可信下游由原始 client cert DER 計算 `x5t#S256` 交由 OpenIddict 驗證；不信其他公開標頭 |
| API Key | 不作認證或授權；有訂閱計量需求時才另外保留 |
| 請求簽章 | HTTP Message Signatures，所有業務 API 呼叫適用；簽章演算法未定，與 mTLS RSA 憑證獨立；HTTP/1.1 僅驗證 Host/raw target/query/body/標頭保留，不宣稱 RFC 9421 驗簽 |
| 簽章驗證 | 最終業務 API 驗證原始呼叫端簽章 |
| 金鑰 | mTLS 與請求簽章私鑰分開；每 Client、每環境獨立 |
| 授權檢查 | Client 的 API／scope 白名單，加業務資料範圍檢查 |
| 業務去重 | 穩定業務識別與 Idempotency Key，保證本地提交不重複 |
| 撤銷時效 | 最多 60 秒內阻擋後續使用已撤銷的 Token／憑證／金鑰／服務身分（含既有長連線，SLA 待驗收） |
| 查證故障 | 無仍在允許期限內的有效快取且無法查證時，拒絕處理（fail closed） |
| 架構與測試 | API First（最小 OpenAPI，不做 codegen）、Reqnroll BDD（生成物在 `obj/` 不追蹤）、EF Core + PostgreSQL、隔離 Testcontainers |

Client Credentials 是取得授權的流程，不是請求簽章方法。mTLS 用戶端認證與憑證綁定 Token 是不同機制，兩者都必須成立。

嚴格區分 mTLS RSA 用戶端憑證與 HTTP Message Signatures 簽章演算法，後者之具體簽章演算法及契約目前未定。Lab 第一版暫以已驗證 RSA-2048 自簽 client cert 為接入範圍，其他憑證類型（如 ECDSA、PKI 階層鏈）明確保留未驗，避免將候選或 proof 誤寫成完整功能。XFCC 解析器目前為 proof 級 regex，已實測缺少/非 PEM/損毀 PEM/缺 Cert/重複標頭/多個 Cert 皆回傳 401，正式實作須保留嚴格解析不變式（拒絕多 Cert、重複標頭、非 PEM，禁止 first-wins）。Proof worktree 27 項情境獨立通過作為 proof 證據，不代表完整 spec AC 已結或 ticket 達成；ECDSA、PKI、憑證輪替、完整 60 秒撤銷生效（含現有連線）、服務故障 fail-closed、signature/nonce/idempotency 皆保留為後續 tickets 驗收；S2、G3 及前四項 XFCC 補測非 red-first，嚴禁補造歷史。

### 身分與信任邊界

每個呼叫服務使用獨立 Client 身分，不共用認證憑證；不同環境不得共用私鑰。

授權伺服器負責用戶端認證、核發 Token 與管理授權。API 的接受判斷必須涵蓋 Token、Client、憑證及請求簽章金鑰的有效狀態。

業務 API mTLS 終止於可信 Gateway（Lab 實作基線選定 Envoy v1.39.3），最小可信邊界契約如下：

- **Caller 認證**：Envoy 驗證呼叫端憑證，使用 RSA-2048 自簽憑證 SHA-256 allowlist（`verify_certificate_hash`，不設 `ACCEPT_UNTRUSTED`）。
- **下游通道認證與防繞過**：Envoy 到業務 API 的通道採獨立 Gateway mTLS 通道，API 以固定（pin）特定 Gateway 憑證 SHA-256 指紋，防範呼叫端直接繞過 Gateway 或非受信 Gateway 呼叫。
- **標頭覆寫與防偽造**：Envoy 設 `forward_client_cert_details: SANITIZE_SET` + `set_current_client_cert_details: {cert: true}` 覆寫 `x-forwarded-client-cert`（XFCC），阻擋外部請求偽造身分。業務 API 不得信任任何其他轉送身分標頭（如 NGINX 樣式指紋標頭）。
- **身分與 Token 綁定比對**：業務 API 僅在可信下游通道中由原始 client cert DER 計算 SHA-256 指紋（`x5t#S256` base64url），交由 OpenIddict 7.7.1 與 token introspection 之 `cnf.x5t#S256` 進行比對。
- **原始請求保留**：Envoy 設 `normalize_path: false`、`merge_slashes: false`、`path_with_escaped_slashes_action: KEEP_UNCHANGED`、未設 host_rewrite，在 HTTP/1.1 下保留客戶端送出之 Host、RawTarget、query、body 與必要簽章標頭逐字保留（不宣稱 RFC 9421 驗簽）。
- **範圍限制**：Token endpoint 採 caller 直接 mTLS 連授權伺服器（未經 Gateway）；此為 Lab 選型，生產級高可用（HA）、動態設定（SDS/xDS）、憑證輪替皆未驗證。

若中介改寫被簽署的目標、標頭或內容，必須定義可驗證的處理方式；不得把改寫後的內容默認為原始呼叫端簽署的內容。

### 認證與授權流程

1. 呼叫服務以 mTLS 向授權伺服器進行 Client Credentials 請求。
2. 授權伺服器驗證 Client 與憑證狀態，依已核准權限核發綁定該憑證的短效 Opaque Token。
3. 呼叫服務經 mTLS 送出 Token 與已簽署的業務請求。
4. 可信驗證邊界查驗 Token 有效性及憑證綁定，最終 API 驗證請求簽章、簽章金鑰歸屬與防重放資訊。
5. 業務 API 檢查目標 API、必要 scope 與可操作的資料範圍。
6. 有副作用的操作通過業務去重判斷後才執行；純查詢不強套業務去重。

Token 到期後重新取得，不以長期 Token 取代生命週期管理。

scope 決定「可做什麼」，業務資料範圍決定「可對哪些資料做」。例如廠商 A 有讀取訂單權限，不代表可以讀取廠商 B 的訂單。不得僅相信 Body 中宣稱的廠商或租戶識別。

### 撤銷與故障處理

撤銷要求涵蓋服務停用、Token 撤銷、mTLS 憑證撤銷及請求簽章金鑰撤銷。

從撤銷操作生效起，到驗證端阻擋後續使用的總延遲，不得超過 60 秒。狀態同步、正向快取與處理延遲都必須納入此上限，不能讓每層各自快取 60 秒。

停用 Client 或輪替憑證，不得被視為既有 Token 已自動失效。已建立的連線也不得因此繞過後續請求的有效狀態判斷。

無法查證且沒有仍符合時效要求的有效快取時，採 fail closed，不執行業務副作用，並明確回報暫時無法驗證的服務錯誤，不假裝成已確認的憑證無效。

此撤銷保證針對後續請求的接受判斷，不代表已提交的業務副作用會被回滾。

### 請求簽章與防重放

採 HTTP Message Signatures，並在實作前制定共同簽章規則，至少涵蓋：

- HTTP 方法與完整目標，包括會影響業務語意的查詢參數。
- 有 Body 時的內容摘要，以及必要的內容型別與標頭。
- 有副作用操作的 Idempotency Key。
- Token 綁定資訊，防止任意替換授權脈絡。
- 簽章時間、接受期限與每次嘗試的新 nonce。

業務 API 必須核對摘要與收到的實際 Body 相符，不能只驗證摘要欄位的簽章。

驗證端必須確認簽章金鑰屬於 Token 所識別的 Client；不得混用不同 Client 各自合法的 Token、憑證與簽章。

nonce 的判斷與登錄必須能防止併發重放，並在所有可接受該請求的執行個體之間維持一致性。僅加入時間戳或 nonce 欄位，不代表已完成防重放。

nonce 保存期必須涵蓋簽章可接受期間與允許的時鐘容差。

合法重試沿用穩定業務識別與 Idempotency Key，但使用新 nonce 重新簽署。已被接受的同一份簽章重送時，應被防重放機制阻擋。

### 業務冪等與本地提交

防重放與業務冪等是兩個不同保證：

- 防重放阻擋同一份合法簽章再次使用。
- 業務冪等阻擋重新簽署、甚至更換請求 Key 後的同一筆業務操作再次產生副作用。

有副作用的 API 必須使用穩定業務識別與 Idempotency Key。識別範圍及內容比對規則由具體 API 定義，避免不同 Client 的識別互相碰撞。

| 情境 | 必要行為 |
|---|---|
| 相同操作識別、相同業務內容，先前已完成 | 不重做副作用，提供既有結果 |
| 相同操作識別、相同內容，仍在處理中 | 不啟動第二次執行，提供處理狀態 |
| 相同業務識別或 Idempotency Key，卻有不同業務內容 | 拒絕，不覆寫既有操作 |
| 更換 Idempotency Key，但仍是相同業務操作 | 仍由穩定業務識別防止重複提交 |
| Idempotency Key 短期紀錄已過期 | 不得因此失去業務唯一性保證 |

業務內容比對不得將每次重試會改變的 nonce、簽章或 Token 視為業務內容變更。

去重狀態與本地業務提交必須一致處理，涵蓋併發、當機及提交後回應遺失的情境。Idempotency Key 保存期至少涵蓋約定的最長重試期；業務唯一性另外維持。

跨服務與外部系統另訂下游冪等協定，本規格不宣稱跨系統 exactly-once。

### 輪替與追查

正常輪替允許新舊憑證或簽章金鑰短暫重疊：先登錄新金鑰、切換呼叫端，再退役舊金鑰。新憑證須重新取得綁定新憑證的 Token，舊 Token 不可搭配新憑證使用。

疑似洩漏時直接撤銷受影響的憑證、金鑰、Token 或 Client，不等待正常輪替的重疊期結束，並維持 60 秒撤銷上限。

追查紀錄包含已驗證的 Client 身分、憑證／簽章金鑰識別、操作、結果及必要關聯識別。不記錄原始 Token、私鑰或完整敏感 Body。

認證失敗時，請求宣稱的 Client 身分只能標為未驗證，不能記成已驗證呼叫者。安全稽核與必要業務去重紀錄分開管理。

## Testing Decisions

好的測試驗證外部可觀察行為：請求是否被接受、錯誤是否明確、資料是否隔離、業務副作用是否只提交一次，以及撤銷是否符合 60 秒上限。不綁定內部類別、函式呼叫順序或資料表實作。

目前工作目錄已有規格文件與領域詞彙表（GLOSSARY.md），但尚無可沿用的業務實作程式碼、測試或 ADR；未對整個 repository 宣稱不存在相關先例。

建議建立一個主要整合測試邊界：從呼叫端經可信 Gateway 到業務 API 的外部行為。測試環境可控制授權與撤銷狀態、查證服務故障、併發及重試，並觀察本地業務提交與追查結果。涵蓋授權伺服器介接、Gateway 驗證、API 授權／驗簽／防重放，以及本地業務去重能力。

使用者已確認此主要整合測試邊界；目前尚未建立此測試能力。不為各內部模組另設多套測試介面。採真實協定與密碼驗證路徑，不以永遠驗證成功的替身證明 mTLS 或簽章保證。

### 驗收情境

以下是未來實作的驗收要求，不代表目前已通過：

| 編號 | 情境 | 預期結果 |
|---|---|---|
| AC-01 | 無有效用戶端憑證請求 Token | 不核發 Token |
| AC-02 | 只持有被偷的 Token，沒有綁定憑證的私鑰 | 無法通過 API 接受判斷 |
| AC-03 | 混用不同 Client 的合法 Token、憑證或簽章 | 拒絕 |
| AC-04 | 撤銷 Token、Client、憑證或簽章金鑰 | 各驗證端含既有連線上的後續請求，最多 60 秒內阻擋 |
| AC-05 | 查證服務故障，沒有符合時效的有效快取 | 拒絕處理，明確回報服務錯誤，無業務副作用 |
| AC-06 | 偽造 Gateway 身分標頭或繞過 Gateway | 不被接受為可信呼叫 |
| AC-07 | 改動已簽署目標、必要標頭或 Body | 簽章／摘要驗證失敗 |
| AC-08 | 同一份簽章向多個執行個體併發重放 | 不重複通過 nonce 接受判斷 |
| AC-09 | 合法重簽重試或併發提交同一業務操作 | 本地副作用只提交一次，回傳既有結果或處理狀態 |
| AC-10 | 相同識別但不同業務內容 | 拒絕，不覆寫 |
| AC-11 | 提交完成但回應遺失，或短期去重紀錄已過期 | 重試不重做同一筆業務副作用 |
| AC-12 | 有 scope，但要求其他廠商／租戶的資料 | 資料範圍授權拒絕 |
| AC-13 | 正常輪替後，拿舊 Token 搭配新憑證 | 拒絕；新憑證取得自己的 Token 後可正常使用 |
| AC-14 | 檢查追查紀錄 | 可辨識已驗證呼叫者，無原始 Token、私鑰或完整敏感 Body |

## Out of Scope

- 代表使用者存取資源、互動式登入與其他 OAuth grant 選型。
- 本次程式碼實作、部署、產品選型與容量規劃。
- 跨服務或外部系統的 exactly-once 保證；另訂下游冪等協定。
- 以 API Key 或 Client Secret 作為額外認證方案。
- Token 與對應私鑰同時外洩時的完整防冒用保證。
- 以請求簽章取代 TLS 或提供資料加密。

## Further Notes

本規格由已確認的訪談決策整理而成，並納入後續指定的 ASP.NET Core 10 平台與 api.template 開發規則，不宣稱目前專案已符合規格。不自行新增依賴產品選型或具體生命週期數值。

本規格已發布至專案的本地 Markdown issue tracker，依既定 triage 名稱標記 `Status: ready-for-agent`，不增加其他 triage，不建立遠端 issue。此狀態代表本次通用設計規格可交接，不代表待具體 API 設計的參數已定案，或已完成實作與驗收。

### 待具體 API 設計的項目

以下刻意未定，不得自行填入預設值並視為已核准：

- 授權核心已選定 OpenIddict 7.7.1，業務 API Gateway Lab 選型已定為 Envoy v1.39.3，Token endpoint 採 caller 直連 mTLS；但 Gateway 生產級配置（高可用 HA、SDS/xDS 動態設定、憑證動態輪替）及 Token endpoint 經 Gateway 路徑目前未定（未驗證）。
- HTTP Message Signatures 簽章演算法、必要欄位、Token 綁定資訊的表示方式與中介改寫處理（獨立於 mTLS RSA 憑證，簽章演算法契約未定）。
- Token 效期、快取配置、撤銷同步機制與容量規劃。
- 簽章接受時間窗、時鐘容差、nonce 儲存方式及保存期。
- 業務識別範圍、內容比對規則、Idempotency Key 保存期。
- 處理中回應、服務錯誤與拒絕情境的具體 HTTP 狀態／回應格式。
- 正常輪替重疊期、稽核保存期及營運流程；其他憑證類型（ECDSA、PKI 階層式憑證鏈等）未驗證。
- 各 API 的最長重試期與下游冪等協定。
- 嚴格記錄：ECDSA、PKI、金鑰輪替、完整 60 秒撤銷生效機制（含現有連線）、服務故障 fail-closed、signature/nonce/idempotency 皆不宣稱 proof 已驗；S2、G3 及前四項 XFCC 補測非 red-first，嚴禁補造歷史。

### 保證限制與參考

Token 與對應私鑰一起外洩，不在「Token 被偷後不能單獨使用」的保證內。請求簽章不取代 TLS，也不提供資料加密。上述組合是本次六項目標的選型，不是所有 server-to-server API 的最低配置。

- [OAuth 2.0 Client Credentials：RFC 6749 §4.4](https://www.rfc-editor.org/rfc/rfc6749.html#section-4.4)
- [Token Introspection：RFC 7662](https://www.rfc-editor.org/rfc/rfc7662.html)
- [mTLS 用戶端認證與憑證綁定 Token：RFC 8705](https://datatracker.ietf.org/doc/html/rfc8705)
- [HTTP Message Signatures：RFC 9421](https://datatracker.ietf.org/doc/html/rfc9421)
- [HTTP 內容摘要：RFC 9530](https://datatracker.ietf.org/doc/html/rfc9530)
