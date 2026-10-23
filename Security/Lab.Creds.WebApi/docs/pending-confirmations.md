# 待使用者確認的 lab 暫定值

本清單整理實作過程中採用、但尚未經使用者核准的暫定值。正式採用前須逐項確認或修改。出處路徑相對於 `.scratch/`（票檔）或專案目錄（ADR）；確認後請把票內「lab 暫定、待使用者確認」改成已確認，並更新對應測試。

## 已確認（不需再確認）

- 服務 Token 效期 300 秒。
- 簽章規則：`ecdsa-p256-sha256`、`created` 不超前 30 秒、`expires` 60 秒。
- 撤銷上限 60 秒（spec 規定）。
- 不實作 Gateway；mTLS 只用自簽憑證模式；狀態儲存為程序內（授權資料庫與訂單儲存另有其他工作線處理）。

## 服務保護（server-to-server-api-protection）

| 項目 | 目前值 | 出處 |
|---|---|---|
| 查證快取上限 | 10 秒 | issues/07、08 |
| 冪等 Key 最長重試期 | 10 分鐘 | issues/06 |
| 冪等 Key 保存期 | 24 小時（業務唯一性永久保存） | issues/06 |
| 處理中租約 | 30 秒 | issues/06、spec |
| 輪替重疊期（建議） | 7 天內完成切換再退役 | issues/08 |
| 安全稽核保存期 | 90 天，僅追加，不經業務 API 公開 | issues/09、15 |
| 錯誤碼與格式 | 503 `verification_unavailable`、`audit_unavailable`、`business_commit_unavailable` | issues/06、07、09 |

## 管理員核准與申請者驗證

| 項目 | 目前值 | 出處 |
|---|---|---|
| 管理介面路徑與格式 | `/admin/*`（信任名單、核准、拒絕、退役、撤銷、稽核查詢） | issues/11–15 |
| 管理員憑證登錄方式 | lab 於環境啟動時登錄；非管理員回 401（不是 403） | issues/11 |
| `/admin/clients/{id}/disable` | spec 未要求，目前保留 | ADR 0001、issues/11、15 |
| 待核准申請上限 | 每個 Client 5 筆（憑證與簽章金鑰合計），超過回 429 `pending_request_limit_reached` | issues/19、21、ADR |
| 待核准申請逾期 | 24 小時，逾期回 409 `request_expired` | issues/19、21、ADR |
| 持有證明時間窗 | 60 秒；nonce 一次性、上限 128 字元 | issues/17、21、ADR |
| 申請欄位名稱 | `possessionProof`、`proofTimestamp`、`proofNonce` | issues/17、21 |
| 內省逾時 | 10 秒 | ADR 0001 |
| 申請稽核標示 | `actorStatus` 一律「未驗證」，另有 `applicantAuthenticated`、`applicantClientId` 區分 | ADR 0001、issues/20 |

## 雙呼叫者（dual-caller-authentication）

| 項目 | 目前值 | 出處 |
|---|---|---|
| 使用者 Token 效期 | 沿用 300 秒 | ADR 0001、issues/02 |
| Token 表示方式 | audience `orders-api-user`、`caller_type=user` | issues/02、03 |
| 驗證路徑分流 | 依是否出示用戶端憑證（有走服務路徑，無走使用者路徑） | issues/02 |
| 使用者防重放 | `X-Request-Nonce` 經 `INonceReplayStore`；缺少或重放 401，狀態故障 503 | issues/05、ADR |
| 使用者資料範圍 | 只能讀寫自己的訂單；他人或服務建立的回 404 | issues/04、ADR |
| 使用者撤銷傳播 | 沿用 60 秒上限；Token 撤銷最多延遲一個快取週期 | issues/06、ADR |
| 錯誤格式 | Token 無效 401、查證故障 503、資料範圍拒絕 404、限定類型操作由另一類型呼叫 403 | ADR |
| 稽核欄位 | `VerifiedClientId` 與 `VerifiedCallerIdentity` 並存（語意重疊） | issues/07、ADR |
| 測試使用者預設權限 | 預設有 `orders.write`，沒有角色區分 | issues/04、ADR |

## 尚未決定（不是數值）

- Client 申請怎麼傳遞給管理員（工單、人工轉交或自助入口）。
- 是否需要多人覆核與管理員角色細分。
- 管理員憑證的頒發與保管。
- 全新 Client 的首次登錄（目前不在範圍）。
- 已通過 mTLS 但越權的申請者，稽核仍標「未驗證」的做法是否接受。
- 「僅服務」的操作與使用者角色模型（目前缺位）。
- 有授權資料庫連線字串（Postgres）時，使用者 Client 註冊路徑是否有測試涵蓋。

## 已知但未解的技術問題

- 「同一簽章併發送往單一執行個體只有一次通過」測試偶發失敗（已見約 4 次，單獨重跑通過，根因未查明）。
- 同一 Client 的兩張 CN 相同的憑證被 OpenIddict 拒絕（ID2197），原因未查明。
