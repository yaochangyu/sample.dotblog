Feature: 雙呼叫者端到端驗收（08 單）
  同一個建立訂單端點同時接受服務呼叫與使用者呼叫：服務與使用者各自成功呼叫；混用 Token、越權資料、重試與撤銷都被正確處理。
  全部經真實 TLS、真實 password grant、真實 client_credentials 與已簽章的服務請求，不使用永遠成功的替身。
  使用者防重放（X-Request-Nonce）與資料範圍皆為 lab 暫定值，待使用者確認。本 lab 以測試使用者帳號代替真實登入流程。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @dual-caller-e2e @isolated
  Scenario: 同一建立訂單端點，服務與使用者各自成功，混用、越權、重試與撤銷都被正確處理
    Given 雙呼叫者票使用者 "test-user-1" 取得使用者 Token
    And 雙呼叫者票使用者 "test-user-2" 取得使用者 Token
    When 雙呼叫者票服務 "orders-client" 以已簽章請求建立訂單 "服務訂單"，Idempotency-Key 為 "e2e-svc-1"
    Then 雙呼叫者票最近一次請求回應 201
    And 雙呼叫者票訂單 "服務訂單" 的持有者為 "orders-client"
    When 雙呼叫者票使用者 "test-user-1" 以使用者 Token 建立訂單 "使用者訂單"，Idempotency-Key 為 "e2e-usr-1"
    Then 雙呼叫者票最近一次請求回應 201
    And 雙呼叫者票訂單 "使用者訂單" 的持有者為 "test-user-1"
    And 雙呼叫者票業務 API 建立訂單總數為 2

    # 混用 Token：服務 Token 不能走使用者路徑，使用者 Token 不能走服務路徑
    When 雙呼叫者票持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求
    Then 雙呼叫者票最近一次請求回應 401
    When 雙呼叫者票以使用者 "test-user-1" 的使用者 Token 附服務憑證與已簽章請求送出建立訂單
    Then 雙呼叫者票最近一次請求回應 401
    And 雙呼叫者票業務 API 建立訂單總數為 2

    # 越權資料：使用者只能讀自己的訂單，他人與服務建立的訂單都回應 404
    When 雙呼叫者票使用者 "test-user-1" 以使用者 Token 查詢訂單 "使用者訂單"
    Then 雙呼叫者票最近一次請求回應 200
    When 雙呼叫者票使用者 "test-user-2" 以使用者 Token 查詢訂單 "使用者訂單"
    Then 雙呼叫者票最近一次請求回應 404
    When 雙呼叫者票使用者 "test-user-1" 以使用者 Token 查詢訂單 "服務訂單"
    Then 雙呼叫者票最近一次請求回應 404

    # 重試：相同 Idempotency-Key 與新 nonce 重送，回傳既有結果，不新增訂單
    When 雙呼叫者票使用者以相同 Idempotency-Key 與新 nonce 重送上一筆建立訂單
    Then 雙呼叫者票最近一次請求回應 201
    And 雙呼叫者票最近一次請求標示為重播
    And 雙呼叫者票重播回應的訂單編號與 "使用者訂單" 相同
    And 雙呼叫者票業務 API 建立訂單總數為 2

    # 撤銷：使用者被停用後，使用者路徑被拒絕且無業務副作用；服務路徑不受影響
    When 雙呼叫者票停用使用者 "test-user-1"
    And 雙呼叫者票使用者 "test-user-1" 以原使用者 Token 建立訂單 "撤銷後訂單"，Idempotency-Key 為 "e2e-usr-2"
    Then 雙呼叫者票最近一次請求回應 401
    And 雙呼叫者票業務 API 建立訂單總數為 2
    When 雙呼叫者票服務 "orders-client" 以已簽章請求建立訂單 "撤銷後服務訂單"，Idempotency-Key 為 "e2e-svc-2"
    Then 雙呼叫者票最近一次請求回應 201
    And 雙呼叫者票業務 API 建立訂單總數為 3

  @record
  Scenario: 08 單驗收項目已逐項勾選且狀態為 resolved
    Then 雙呼叫者票第 1 項驗收已勾選
    And 雙呼叫者票第 2 項驗收已勾選
    And 雙呼叫者票第 3 項驗收已勾選
    And 雙呼叫者票第 4 項驗收已勾選
    And 雙呼叫者票第 5 項驗收已勾選
    And 雙呼叫者票第 6 項驗收已勾選
    And 雙呼叫者票的狀態為 resolved
    And 雙呼叫者票的實作紀錄包含 "lab 暫定、待使用者確認"
