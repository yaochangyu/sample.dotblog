Feature: 業務 API 驗證原始呼叫端的 RFC 9421 請求簽章 (Ticket 03)
  作為合作廠商提交資料 API 的維護者
  我希望每個業務請求都帶有該 Client 專屬金鑰的簽章
  以便被竄改、混用身分或缺少簽章的請求不會進入業務處理
  註：本階段只驗證簽章格式、時間窗與內容，不做 nonce 唯一性檢查；時間窗內重送仍會被接受，防重放由 Ticket 04 處理

  Scenario Outline: 經 Gateway 送出合法簽章的各種請求形狀都能通過
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出 "<method>" 到 "<target>" 且 "<body>" 的合法簽章請求
    Then API 回應 200 且驗證的簽章金鑰為 "partner-a-sig-1"
    And 業務處理被執行 1 次

    Examples:
      | method | target                             | body |
      | POST   | /partner/inspect/orders?tenant=acme | 有本文 |
      | POST   | /partner/inspect/orders?tenant=acme | 無本文 |
      | PUT    | /partner/inspect/orders/1           | 有本文 |
      | PATCH  | /partner/inspect/orders/1?x=1&x=2   | 有本文 |
      | DELETE | /partner/inspect/orders/1           | 無本文 |
      | DELETE | /partner/inspect/orders/1?force=1   | 有本文 |
      | GET    | /partner/inspect/orders?tenant=acme | 無本文 |
      | GET    | /partner/inspect/orders             | 無本文 |

  Scenario: 簽章的 GET 讀取請求回報由簽章確認的 Client
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出已簽章的 GET whoami 請求
    Then API 回應 200 且回報 Client 為 "partner-a"

  Scenario: 有效 token 與憑證但完全沒有簽章被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出簽章後被改為 "缺少簽章標頭" 的請求
    Then API 回應 401 且原因為 "signature_missing"
    And 業務處理未被執行

  Scenario Outline: 簽章後被竄改或不符合 Lab Profile 的請求被拒絕且不進入業務處理
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    And 該服務另外取得第二個有效 token
    When 該服務經 Gateway 送出簽章後被改為 "<tamper>" 的請求
    Then API 回應 401 且原因為 "<reason>"
    And 業務處理未被執行

    Examples:
      | tamper                          | reason                        |
      | 只有 Signature-Input 沒有 Signature | signature_missing             |
      | 簽章值被改動                        | signature_invalid             |
      | 簽章長度不是 64 bytes              | signature_malformed           |
      | 使用未登記的金鑰                      | key_unknown                   |
      | 使用另一個 Client 的合法金鑰            | key_client_mismatch           |
      | 使用已停用的金鑰                      | key_inactive                  |
      | 登記的 keyid 但使用不同私鑰              | signature_invalid             |
      | 演算法不是 ecdsa-p256-sha256        | algorithm_unsupported         |
      | 缺少 nonce 參數                    | signature_parameters_invalid  |
      | 參數順序錯誤                        | signature_parameters_invalid  |
      | 簽章標籤不是 sig1                   | signature_label_unsupported   |
      | 覆蓋元件缺少 authorization           | components_mismatch           |
      | 覆蓋元件順序錯誤                      | components_mismatch           |
      | 覆蓋元件多出 @scheme                 | components_mismatch           |
      | 簽章已過期                          | signature_expired             |
      | 簽章尚未生效                         | signature_not_yet_valid       |
      | 有效期超過 60 秒                     | signature_time_invalid        |
      | nonce 格式錯誤                      | nonce_invalid                 |
      | 本文被竄改                          | content_digest_mismatch       |
      | Content-Digest 使用 sha-512       | content_digest_invalid        |
      | 移除 Content-Digest               | header_missing                |
      | 查詢參數被竄改                        | signature_invalid             |
      | 路徑被替換                          | signature_invalid             |
      | 方法被替換                          | signature_invalid             |
      | authority 被替換                   | signature_invalid             |
      | Authorization 被換成另一個有效 token  | signature_invalid             |
      | Idempotency-Key 被替換             | signature_invalid             |
      | Content-Type 被替換                | signature_invalid             |
      | 移除 Idempotency-Key              | header_missing                |

  Scenario: partner-b 的 token 搭配 partner-a 的合法金鑰被拒絕
    Given 呼叫服務 "partner-b" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出簽章後被改為 "使用另一個 Client 的合法金鑰" 的請求
    Then API 回應 401 且原因為 "key_client_mismatch"
    And 業務處理未被執行

  Scenario: GET 請求帶有本文被明確拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出簽章後被改為 "GET 帶有本文" 的請求
    Then API 回應 401 且原因為 "body_not_allowed"
    And 業務處理未被執行

  Scenario: 簽章在有效時間窗內重送仍被接受 (已知限制，Ticket 04 才處理 nonce 防重放)
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 重送完全相同的已簽章請求兩次
    Then 兩次回應皆為 200
    And 業務處理被執行 2 次

  Scenario: 不經 Gateway 直接呼叫 API 同樣必須簽章
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務直接對 API 送出簽章後被改為 "缺少簽章標頭" 的請求
    Then API 回應 401 且原因為 "signature_missing"
    And 業務處理未被執行

  Scenario: 不經 Gateway 直接呼叫 API 被竄改的簽章被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務直接對 API 送出簽章後被改為 "本文被竄改" 的請求
    Then API 回應 401 且原因為 "content_digest_mismatch"
    And 業務處理未被執行

  Scenario Outline: 以實際接收的 bytes 判斷本文，chunked 與空本文的處理一致
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務直接對 API 送出 "<shape>" 的 chunked 簽章請求
    Then API 回應 <status> 且原因為 "<reason>"

    Examples:
      | shape                  | status | reason                |
      | 非空本文並依有本文簽署      | 200    | none                  |
      | 空本文並依無本文簽署       | 200    | none                  |
      | 非空本文但依無本文簽署      | 401    | components_mismatch   |
      | 空本文但依有本文簽署       | 401    | components_mismatch   |

  Scenario: 被拒絕的請求日誌不含 token、簽章值與 signature base
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 送出簽章後被改為 "簽章值被改動" 的請求
    Then API 回應 401 且原因為 "signature_invalid"
    And 日誌只記錄 keyid 與原因碼而不含 token、Authorization、簽章值與 signature base
