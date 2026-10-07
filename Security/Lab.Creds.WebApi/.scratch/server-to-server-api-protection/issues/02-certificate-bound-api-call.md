# 02: 呼叫服務取得 Token 並經可信入口呼叫 API

**What to build:** 呼叫服務以獨立 Client 身分取得 Token，經可信 Gateway 呼叫 ASP.NET Core 10 示範 API；合法服務可取得回應，沒有對應憑證的呼叫不能使用 Token。本切片僅供受控測試環境，尚未完成全部請求保護。

**Blocked by:** 01 — 確認 auth 技術選型與接入契約。

**Status:** ready-for-agent

- [ ] 採用 01 核准方案與開發方式，建立呼叫端 → 可信 Gateway → 業務 API 的主要整合測試邊界。
- [ ] 每個示範呼叫服務、環境使用獨立 Client 與憑證；不以 API Key 或 Client Secret 提供替代認證入口。
- [ ] mTLS 用戶端認證成功才核發綁定該憑證的短效 Opaque Token；Token 效期已確認為 5 分鐘（使用者決策：到期後重新以 Client Credentials 取得，不使用 Refresh Token，不取代 60 秒撤銷要求）。
- [ ] Token 到期後可重新取得；缺少或無效憑證不能取得 Token。
- [ ] 經受保護且已認證的 introspection 介面判斷 Token 有效性、有效期限、目標 API 與憑證綁定，不只看 active。
- [ ] 只持有 Token、使用不同 Client 的憑證、錯誤目標 API 或無效 Token 時拒絕呼叫。
- [ ] Gateway 下游通道經過認證，外部偽造的驗證資訊被移除或覆寫，業務 API 不能繞過入口。
- [ ] 合法呼叫建立已驗證 Client 身分，不採信 Body 或外部標頭自行宣稱的身分。
- [ ] 示範流程與測試可重現，明確說明此階段尚未具備簽章、防重放及完整業務授權。

**階段限制**：本切片僅示範受控環境的憑證綁定 Token 與可信入口；尚無 HTTP Message Signatures、nonce/防重放、Idempotency、完整業務授權、60 秒撤銷 SLA、HA/SDS 與憑證輪替（後續 tickets）。`/partner/inspect` 為 proof 診斷端點。

對應驗收：AC-01、AC-02、AC-03 的 Token／憑證部分、AC-06。
