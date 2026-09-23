# language: zh-TW
功能: Admin API Key 頒發機制
  身為系統管理員，我可以透過 /api/admin/clients 核發與查詢 ApiKeyClient。
  這是內部管理工具，只在 Development 環境且帶正確 X-Admin-Key 時才能存取，其餘一律拒絕。

  場景: 管理員帶正確 X-Admin-Key 成功核發新 Key
    假設 我帶正確的 X-Admin-Key
    當 我送出 POST /api/admin/clients 請求，ClientName 為 "Partner Alpha"
    那麼 Admin 回應狀態碼應該是 200
    而且 回應內容應該包含 clientName "Partner Alpha"
    而且 回應內容應該包含 apiKey 欄位
    而且 回應內容應該包含 secret 欄位

  場景: 查詢清單不包含 Secret 欄位
    假設 我帶正確的 X-Admin-Key
    而且 我已經核發了一組新的 Client，ClientName 為 "Partner Beta"
    當 我送出 GET /api/admin/clients 請求
    那麼 Admin 回應狀態碼應該是 200
    而且 回應內容應該包含 clientName "Partner Beta"
    而且 回應內容不應該包含 secret 欄位

  場景: 缺少 X-Admin-Key 應該回 401
    假設 我沒有帶 X-Admin-Key
    當 我送出 POST /api/admin/clients 請求，ClientName 為 "Partner Gamma"
    那麼 Admin 回應狀態碼應該是 401

  場景: 錯誤的 X-Admin-Key 應該回 401
    假設 我帶錯誤的 X-Admin-Key
    當 我送出 POST /api/admin/clients 請求，ClientName 為 "Partner Delta"
    那麼 Admin 回應狀態碼應該是 401

  場景: 非 Development 環境呼叫 Admin API 應該回 404
    假設 系統執行在非 Development 環境
    當 我送出 POST /api/admin/clients 請求，ClientName 為 "Partner Epsilon"
    那麼 Admin 回應狀態碼應該是 404
