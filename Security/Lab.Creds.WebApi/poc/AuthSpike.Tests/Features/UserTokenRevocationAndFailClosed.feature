Feature: 使用者 Token 撤銷與查證故障時 fail closed（06 單）
  依 06 單驗收項目：使用者 Token 被撤銷或使用者被停用後，既有撤銷上限（60 秒）內的後續請求被拒絕（AC-D11）；
  使用者 Token 的查證服務故障且沒有有效快取時，拒絕處理並明確回報 503 verification_unavailable，無業務副作用（AC-D12）。
  故障回應與 Token 無效的 401 明確區分；使用者撤銷不影響服務路徑。本 lab 以測試使用者帳號代替真實登入流程。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @record
  Scenario: 06 單行為驗收項目已勾選且實作紀錄標註 lab 暫定值
    Then 撤銷票第 1 項驗收已勾選
    And 撤銷票第 2 項驗收已勾選
    And 撤銷票第 3 項驗收已勾選
    And 撤銷票第 4 項驗收已勾選
    And 撤銷票第 5 項驗收已勾選
    And 撤銷票的實作紀錄包含 "lab 暫定、待使用者確認"

  @user-revocation @isolated
  Scenario: AC-D11 使用者 Token 被撤銷後，撤銷上限內的後續查詢與建立訂單請求被拒絕且無業務副作用
    Given 撤銷票使用者 "test-user-1" 取得使用者 Token 並成功建立訂單 "撤銷前訂單"
    When 撤銷票管理者撤銷使用者 Token
    Then 撤銷票使用者查詢於 60 秒內回應 401
    And 撤銷票實測的撤銷生效延遲小於 60 秒
    When 撤銷票使用者以使用者 Token 送出建立訂單請求
    Then 撤銷票最近一次請求回應 401
    And 撤銷票業務 API 建立訂單總數為 1

  @user-revocation @isolated
  Scenario: AC-D11 使用者被停用後，既有使用者 Token 的後續查詢於撤銷上限內被拒絕
    Given 撤銷票使用者 "test-user-1" 取得使用者 Token 並成功建立訂單 "停用前訂單"
    When 撤銷票測試使用者 "test-user-1" 被停用
    Then 撤銷票使用者查詢於 60 秒內回應 401
    And 撤銷票實測的撤銷生效延遲小於 60 秒
    When 撤銷票使用者以使用者 Token 送出建立訂單請求
    Then 撤銷票最近一次請求回應 401
    And 撤銷票業務 API 建立訂單總數為 1

  @user-revocation @isolated
  Scenario: AC-D11 使用者 Token 被撤銷不影響服務路徑，服務 Token 的已簽章查詢仍回應 200
    Given 撤銷票服務 "orders-client" 以憑證取得服務 Token 並建立訂單 "服務訂單"
    And 撤銷票使用者 "test-user-1" 取得使用者 Token 並成功建立訂單 "使用者訂單"
    When 撤銷票管理者撤銷使用者 Token
    Then 撤銷票使用者查詢於 60 秒內回應 401
    When 撤銷票服務 "orders-client" 以已簽章請求查詢訂單 "服務訂單"
    Then 撤銷票最近一次請求回應 200

  @user-revocation @isolated
  Scenario: AC-D12 使用者查證服務故障且無有效快取時拒絕處理，回報 503 且無業務副作用
    Given 撤銷票使用者 "test-user-1" 取得使用者 Token
    And 撤銷票授權伺服器停止服務
    When 撤銷票使用者以使用者 Token 送出建立訂單請求
    Then 撤銷票最近一次請求回應 503 且錯誤代碼為 "verification_unavailable"
    And 撤銷票最近一次回應不是 Token 無效的 401
    And 撤銷票業務 API 建立訂單總數為 0

  @user-revocation @short-cache
  Scenario: AC-D12 使用者查證快取有效期間內可接受，故障後快取過期即回報 503 且失敗不延長快取期限
    Given 撤銷票使用者 "test-user-1" 取得使用者 Token 並成功建立訂單 "快取前訂單"
    And 撤銷票授權伺服器停止服務
    When 撤銷票在快取有效期限內以使用者 Token 查詢訂單 "快取前訂單"
    Then 撤銷票最近一次請求回應 200
    And 撤銷票快取命中的查詢在 2 秒內完成
    When 撤銷票等待使用者查證快取過期後連續送出 2 次使用者查詢
    Then 撤銷票所有使用者查詢回應為 503 且錯誤代碼為 "verification_unavailable"
    And 撤銷票使用者查詢回應不是 Token 無效的 401
