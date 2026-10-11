Feature: 持有證明的綁定、時間窗與一次性 nonce
  依 17、18 單的持有證明補強：簽署內容綁定已驗證的申請者 Client、申請者目前憑證指紋、所申請的新憑證或金鑰指紋、時間戳與一次性 nonce。
  伺服器只接受時間窗內（lab 暫定 60 秒，待使用者確認）且 nonce 未使用過的證明；同一份證明重送被拒絕，且不新增待核准紀錄、不佔額度；
  證明內容與申請者身分不符被拒絕。失敗回應沿用 400 possession_proof_invalid，被拒絕的嘗試仍寫入稽核。

  Background:
    Given 授權伺服器與建立訂單 API 已啟動
    And 已登錄服務 "orders-client" 與其用戶端憑證
    And 已登錄服務 "orders-partner-client" 與其用戶端憑證

  Scenario Outline: 持有證明的時間戳依與伺服器時間的差距決定是否受理
    Given 持有證明綁定情境中記錄 "orders-client" 的已受理申請數
    When 以 "orders-client" 身分為 "orders-client" 送出時間偏移 <偏移> 秒的持有證明新憑證申請
    Then 持有證明申請回應為 <回應>
    And "orders-client" 的已受理申請數相對基準增加 <增加>

    Examples:
      | 偏移 | 回應 | 增加 |
      | -61  | 400  | 0    |
      | 61   | 400  | 0    |
      | -300 | 400  | 0    |
      | -59  | 202  | 1    |
      | 59   | 202  | 1    |

  Scenario: 同一份有效證明重送被拒絕且不新增待核准紀錄
    Given 持有證明綁定情境中記錄 "orders-client" 的已受理申請數
    When 以 "orders-client" 身分為 "orders-client" 送出持有證明新憑證申請並保留該證明
    Then 持有證明申請回應為 202
    When 以相同的持有證明重送新憑證申請
    Then 持有證明申請回應為 400
    And "orders-client" 的已受理申請數相對基準增加 1
    And "orders-client" 的持有證明拒絕稽核數增加 1

  Scenario: 持有證明綁定的申請者與出示的憑證不符被拒絕
    Given 持有證明綁定情境中記錄 "orders-client" 的已受理申請數
    When 以 "orders-client" 身分為 "orders-client" 送出綁定 "orders-partner-client" 申請者指紋的持有證明新憑證申請
    Then 持有證明申請回應為 400
    And "orders-client" 的已受理申請數相對基準增加 0

  Scenario: 缺少時間戳與 nonce 的持有證明被拒絕
    Given 持有證明綁定情境中記錄 "orders-client" 的已受理申請數
    When 以 "orders-client" 身分為 "orders-client" 送出缺漏時間戳與 nonce 的持有證明新憑證申請
    Then 持有證明申請回應為 400
    And "orders-client" 的已受理申請數相對基準增加 0

  Scenario: 簽章金鑰申請的同一份持有證明重送被拒絕且不新增待核准紀錄
    Given 持有證明綁定情境中記錄 "orders-client" 的已受理申請數
    When 以 "orders-client" 身分為 "orders-client" 送出持有證明簽章金鑰申請並保留該證明
    Then 持有證明申請回應為 202
    When 以相同的持有證明重送簽章金鑰申請
    Then 持有證明申請回應為 400
    And "orders-client" 的已受理申請數相對基準增加 1

  @record
  Scenario: 持有證明規格與 ADR 已記錄綁定、時間窗與 nonce 的 lab 暫定值
    Then 持有證明規格文件包含 "一次性 nonce"
    And 持有證明規格文件包含 "持有證明時間窗 60 秒"
    And 持有證明 ADR 包含 "持有證明 nonce 一次性"
    And 持有證明 ADR 包含 "持有證明時間窗 60 秒"
