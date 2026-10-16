Feature: 冪等與防重放依呼叫者身分分開（05 單）
  同一業務識別由使用者與服務各自提交時互不覆蓋；不同使用者的相同業務識別互不衝突；
  使用者重試或併發提交同一業務操作時本地副作用只提交一次；使用者寫入沒有請求簽章，仍以防重放 nonce 保護。
  使用者防重放機制（X-Request-Nonce 標頭）為 lab 暫定值，待使用者確認；服務路徑既有行為不變。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  @isolated
  Scenario: AC-D09 使用者與服務各自提交同一業務識別時各自獨立，互不覆蓋也不誤判為重複
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    And 服務 "orders-client" 以授權範圍 "orders.read orders.write" 取得服務 Token（05 單）
    When 使用者 "test-user-1" 以 Idempotency-Key "k-user-shared" 送出業務識別 "shared-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    When 服務 "orders-client" 以 Idempotency-Key "k-service-shared" 已簽章送出業務識別 "shared-ref"，品項 "pen"，數量 3（05 單）
    Then 05 單最近一次請求回應 201
    And 05 單業務識別 "shared-ref" 在使用者 "test-user-1" 範圍的訂單數為 1
    And 05 單業務識別 "shared-ref" 在服務 "orders-client" 範圍的訂單數為 1

  @isolated
  Scenario: 不同使用者提交相同業務識別互不衝突，同一使用者範圍內業務識別仍唯一
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    And 使用者 "test-user-2" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 以 Idempotency-Key "k-u1-first" 送出業務識別 "common-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    When 使用者 "test-user-2" 以 Idempotency-Key "k-u2-first" 送出業務識別 "common-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    And 05 單業務識別 "common-ref" 在使用者 "test-user-2" 範圍的訂單數為 1
    When 使用者 "test-user-1" 以 Idempotency-Key "k-u1-second" 送出業務識別 "common-ref"，品項 "pen"，數量 3（05 單）
    Then 05 單最近一次請求回應 422
    And 05 單最近一次回應錯誤代碼為 "order_reference_conflict"
    And 05 單業務識別 "common-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @isolated
  Scenario: AC-D10 使用者以相同 Idempotency-Key 與新 nonce 重試時回傳既有結果，本地副作用只提交一次
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 以 Idempotency-Key "k-retry" 送出業務識別 "retry-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    When 使用者 "test-user-1" 以相同 Idempotency-Key 與新 nonce 重送上一筆業務操作（05 單）
    Then 05 單最近一次請求回應 201
    And 05 單最近一次請求標示為重播
    And 05 單重播回應的訂單編號與首次建立相同
    And 05 單業務識別 "retry-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @isolated
  Scenario: AC-D10 使用者的相同業務操作仍處理中時回應處理中狀態，且不啟動第二次提交
    Given 使用者業務提交將暫停（05 單）
    And 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 以 Idempotency-Key "k-hold-first" 於背景送出業務識別 "hold-ref"，品項 "book"，數量 1（05 單）
    And 等待使用者背景送出進入業務提交（05 單）
    When 使用者 "test-user-1" 以 Idempotency-Key "k-hold-second" 送出業務識別 "hold-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 409
    When 放行使用者暫停的業務提交（05 單）
    Then 使用者背景送出的請求回應 201（05 單）
    And 05 單業務識別 "hold-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @isolated
  Scenario: AC-D10 使用者併發提交同一業務操作時，本地副作用只提交一次
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 以 10 組新 nonce 與新 Idempotency-Key 併發送出業務識別 "race-ref"，品項 "book"，數量 1（05 單）
    Then 05 單併發結果只包含回應 201 或 409
    And 05 單併發結果中建立的訂單編號只有 1 個
    And 05 單業務識別 "race-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @isolated
  Scenario: 使用者寫入缺少防重放 nonce 時被拒絕，且無業務副作用
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 不附 X-Request-Nonce 送出業務識別 "no-nonce-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 401
    And 05 單業務識別 "no-nonce-ref" 在使用者 "test-user-1" 範圍的訂單數為 0

  @isolated
  Scenario: 使用者寫入重放相同 nonce 時被拒絕，且不產生第二筆業務副作用
    Given 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 使用者 "test-user-1" 以 nonce "user-nonce-1" 送出業務識別 "replay-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    When 使用者 "test-user-1" 以 nonce "user-nonce-1" 重送上一筆使用者寫入請求（05 單）
    Then 05 單最近一次請求回應 401
    And 05 單業務識別 "replay-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @isolated
  Scenario: 服務路徑既有的 nonce 防重放與冪等行為不因使用者路徑改變
    Given 服務 "orders-client" 以授權範圍 "orders.read orders.write" 取得服務 Token（05 單）
    And 使用者 "test-user-1" 取得使用者 Token（05 單）
    When 服務 "orders-client" 以 nonce "svc-nonce-1" 已簽章送出業務識別 "svc-ref"，Idempotency-Key 為 "k-svc-1"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    When 服務 "orders-client" 重送上一筆已簽章請求（相同 nonce）（05 單）
    Then 05 單最近一次請求回應 401
    When 使用者 "test-user-1" 以 Idempotency-Key "k-user-after-svc" 送出業務識別 "svc-ref"，品項 "book"，數量 1（05 單）
    Then 05 單最近一次請求回應 201
    And 05 單業務識別 "svc-ref" 在服務 "orders-client" 範圍的訂單數為 1
    And 05 單業務識別 "svc-ref" 在使用者 "test-user-1" 範圍的訂單數為 1

  @record
  Scenario: 05 單驗收項目已逐項勾選、狀態為 resolved，且防重放機制已標註為 lab 暫定待使用者確認
    Then 冪等與防重放票第 1 項驗收已勾選
    And 冪等與防重放票第 2 項驗收已勾選
    And 冪等與防重放票第 3 項驗收已勾選
    And 冪等與防重放票第 4 項驗收已勾選
    And 冪等與防重放票第 5 項驗收已勾選
    And 冪等與防重放票第 6 項驗收已勾選
    And 冪等與防重放票的狀態為 resolved
    And 冪等與防重放票的實作紀錄標註防重放機制為 lab 暫定待使用者確認
