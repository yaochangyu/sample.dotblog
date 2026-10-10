Feature: 業務操作重試與併發只提交一次
  依 06 單驗收項目，確認已授權且通過請求保護的同一筆建立訂單操作，
  因重試、併發、提交前後當機或回應遺失而重複送出時，本地業務副作用只提交一次。
  穩定業務識別為 client_id 加 orderReference；Idempotency-Key 只定位請求嘗試。
  本 lab 不實作 Gateway，呼叫端直接呼叫業務 API；保證範圍僅限本地提交，不宣稱下游或跨系統 exactly-once。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證

  Scenario: 業務識別、Idempotency Key 範圍與保存期已記錄為 lab 暫定值，且契約涵蓋業務識別
    Then 06 單第 1 項驗收已勾選
    And 06 單的實作紀錄包含 "lab 暫定、待使用者確認"
    And 06 單的實作紀錄包含 "業務識別為 client_id 加 orderReference"
    And 06 單的實作紀錄包含 "最長重試期 10 分鐘"
    And 06 單的實作紀錄包含 "Idempotency-Key 保存 24 小時"
    And 06 單的實作紀錄包含 "業務內容比對為 item 與 quantity"
    And 06 單的實作紀錄包含 "排除 nonce、簽章與 Token"
    And 建立訂單 API 契約包含 "orderReference"
    And 建立訂單 API 契約包含 "business_commit_unavailable"

  @isolated
  Scenario: 相同業務識別與相同內容的已完成操作回傳既有結果，不重做副作用
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "complete-key" 送出訂單 "ref-complete"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 簽章呼叫端以相同 Idempotency-Key 與新 nonce 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 最近一次訂單請求標示為重播
    And 重播回應的訂單編號與首次建立相同
    And 訂單 "ref-complete" 的業務訂單數為 1
    And 06 單第 2 項驗收已勾選

  @isolated
  Scenario: 相同操作仍處理中時回應處理中狀態，且不啟動第二次執行
    Given 下一次業務提交將暫停
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "proc-first" 於背景送出訂單 "ref-processing"，品項 "book"，數量 1
    And 等待背景送出進入業務提交
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "proc-second" 送出訂單 "ref-processing"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 409
    And 最近一次訂單請求的處理狀態為 "processing"
    When 放行暫停的業務提交
    Then 背景送出的訂單請求回應 201
    And 訂單 "ref-processing" 的業務訂單數為 1
    And 06 單第 3 項驗收已勾選

  Scenario: 相同 Idempotency Key 但業務內容不同時明確拒絕，且不覆寫既有操作
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "conflict-key" 送出訂單 "ref-conflict-key"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "conflict-key" 送出訂單 "ref-conflict-key-other"，品項 "pen"，數量 3
    Then 最近一次訂單請求回應 422
    And 最近一次訂單請求的錯誤代碼為 "idempotency_key_conflict"
    And 訂單 "ref-conflict-key-other" 的業務訂單數為 0
    And 06 單第 4 項驗收已勾選

  Scenario: 相同業務識別但業務內容不同時明確拒絕，且不覆寫既有訂單
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "identity-first" 送出訂單 "ref-identity"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "identity-second" 送出訂單 "ref-identity"，品項 "book"，數量 2
    Then 最近一次訂單請求回應 422
    And 最近一次訂單請求的錯誤代碼為 "order_reference_conflict"
    And 訂單 "ref-identity" 的內容為品項 "book" 數量 1
    And 06 單第 4 項驗收已勾選

  Scenario: 更換 Idempotency Key 仍是相同業務操作時，由業務識別阻止重複提交
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "swap-first" 送出訂單 "ref-swap"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 簽章呼叫端以新的 Idempotency-Key "swap-second" 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 最近一次訂單請求標示為重播
    And 訂單 "ref-swap" 的業務訂單數為 1
    And 06 單第 5 項驗收已勾選

  Scenario: 業務內容比對排除重試會改變的 nonce、簽章與 Token
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "content-key" 送出訂單 "ref-content"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 簽章呼叫端以新取得的 Token、新 nonce 與相同 Idempotency-Key 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 重送使用的 Token 與原 Token 不同
    And 重送使用的 nonce 與原請求不同
    And 最近一次訂單請求標示為重播
    And 訂單 "ref-content" 的業務訂單數為 1
    And 06 單第 6 項驗收已勾選

  @isolated
  Scenario: 提交前當機不產生訂單，租約到期後重試只提交一次
    Given 下一次業務提交於寫入前當機
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "crash-before" 送出訂單 "ref-crash-before"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 503
    And 最近一次訂單請求的錯誤代碼為 "business_commit_unavailable"
    And 訂單 "ref-crash-before" 的業務訂單數為 0
    When 簽章呼叫端以新的 Idempotency-Key "crash-before-retry" 重送上一筆訂單
    Then 最近一次訂單請求回應 409
    When 業務操作租約到期
    And 簽章呼叫端以新的 Idempotency-Key "crash-before-resume" 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 訂單 "ref-crash-before" 的業務訂單數為 1
    And 06 單第 7 項驗收已勾選

  @isolated
  Scenario: 提交後回應遺失時重試回傳既有結果，且只提交一次
    Given 下一次業務提交於寫入後回應遺失
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "lost-first" 送出訂單 "ref-lost"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 503
    And 訂單 "ref-lost" 的業務訂單數為 1
    When 簽章呼叫端以相同 Idempotency-Key 與新 nonce 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 最近一次訂單請求標示為重播
    And 訂單 "ref-lost" 的業務訂單數為 1
    And 06 單第 7 項驗收已勾選

  @isolated
  Scenario: 跨執行個體併發提交同一業務操作只提交一次
    Given 第二個建立訂單 API 執行個體已啟動並共用防重放儲存
    When 簽章呼叫端 "orders-client" 以 10 組新簽章與新 Idempotency-Key 併發送出訂單 "ref-race"，品項 "book"，數量 1，交替送往兩個執行個體
    Then 併發結果只包含回應 201 或 409
    And 併發結果中建立的訂單編號只有 1 個
    And 訂單 "ref-race" 的業務訂單數為 1
    And 06 單第 7 項驗收已勾選

  @isolated
  Scenario: Idempotency Key 保存期涵蓋最長重試期，紀錄過期後仍維持業務唯一性
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "retention-key" 送出訂單 "ref-retention"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    And Idempotency-Key "retention-key" 的保存至時間不早於首次送出加 10 分鐘
    When 業務操作的 Idempotency-Key 紀錄已過期
    And 簽章呼叫端以相同 Idempotency-Key 與新 nonce 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 最近一次訂單請求標示為重播
    And 訂單 "ref-retention" 的業務訂單數為 1
    And 06 單第 8 項驗收已勾選

  @isolated
  Scenario: 持久訂單與查詢結果證明只提交一次，而非僅以回應相同為證據
    Given 下一次業務提交於寫入後回應遺失
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "evidence-first" 送出訂單 "ref-evidence"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 503
    When 簽章呼叫端以新的 Idempotency-Key "evidence-second" 重送上一筆訂單
    Then 最近一次訂單請求回應 201
    And 訂單 "ref-evidence" 的業務訂單數為 1
    And 以最近一次成功回應的訂單編號查詢，回應 200 且品項為 "book"，數量為 1
    And 06 單第 9 項驗收已勾選

  Scenario: 純查詢不套用業務去重，查詢不寫入去重紀錄
    When 簽章呼叫端 "orders-client" 以 Idempotency-Key "query-key" 送出訂單 "ref-query"，品項 "book"，數量 1
    Then 最近一次訂單請求回應 201
    When 記錄目前的業務去重紀錄數
    And 簽章呼叫端查詢上一筆訂單兩次
    Then 兩次查詢皆回應 200
    And 業務去重紀錄數仍與記錄時相同
    And 06 單第 9 項驗收已勾選

  Scenario: 本地提交保證與下游冪等責任分開記錄
    Then 06 單的實作紀錄包含 "本地提交保證"
    And 06 單的實作紀錄包含 "不宣稱下游或跨系統 exactly-once"
    And 06 單的實作紀錄包含 "下游冪等協定"
    And 06 單第 10 項驗收已勾選
