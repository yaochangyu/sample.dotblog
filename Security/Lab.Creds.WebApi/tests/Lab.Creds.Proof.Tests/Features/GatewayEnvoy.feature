Feature: Envoy Gateway 終止 mTLS 後 API 仍以原始 Client 憑證判斷 (Ticket 01 Gateway proof)
  作為合作廠商提交資料 API 的維護者
  我希望 Gateway 終止 mTLS 後，API 仍只信任 Gateway 傳來的原始 Client 憑證
  以便 token 綁定的是合作廠商憑證，而不是 Gateway 憑證

  Scenario: 合法呼叫端經 Envoy Gateway 以同一憑證和 token 提交資料成功
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務以同一憑證和 token 經 Gateway 對 API 提交合作廠商資料
    Then Gateway 後的 API 回應 202 並回報已驗證 Client 為 "partner-a"

  Scenario: 沒有出示憑證的呼叫端被 Gateway 拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 攻擊者只帶 token 且不出示憑證經 Gateway 提交合作廠商資料
    Then 連線在到達 API 之前即被 Gateway 拒絕

  Scenario: 未登錄於 Gateway 允許清單的憑證被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有未註冊憑證的呼叫端帶著 partner-a 的 token 經 Gateway 提交合作廠商資料
    Then 連線在到達 API 之前即被 Gateway 拒絕

  Scenario: 允許清單內的另一張憑證帶著 partner-a 的 token 被 API 以 cnf 不符拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有 "partner-b" 憑證的呼叫端帶著 partner-a 的 token 經 Gateway 提交合作廠商資料
    Then Gateway 後的 API 回應 401

  Scenario: 偽造 X-Forwarded-Client-Cert 冒充 partner-a 憑證仍被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有 "partner-b" 憑證的呼叫端帶著 partner-a 的 token 並偽造聲稱為 partner-a 憑證的轉送標頭經 Gateway 提交合作廠商資料
    Then Gateway 後的 API 回應 401

  Scenario: Client 繞過 Gateway 以自己的憑證直連 Gateway 後的 API 被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務以同一憑證和 token 繞過 Gateway 直連 API 提交合作廠商資料
    Then API 拒絕該連線

  Scenario: 非受信 Gateway 憑證偽造轉送標頭直連 API 被拒絕
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 持有非受信 Gateway 憑證的呼叫端偽造 partner-a 憑證的轉送標頭直連 API 提交合作廠商資料
    Then API 拒絕該連線

  Scenario: API 由 Gateway 轉送的原始 Client 憑證計算出的 x5t#S256 等於 token cnf 而非 Gateway 憑證
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務以同一憑證和 token 經 Gateway 查詢 API 所見的請求資訊
    Then API 所見的 Client 憑證 x5t#S256 等於 token 的 cnf 與 partner-a 憑證指紋
    And API 所見的 Client 憑證 x5t#S256 不等於 Gateway 憑證指紋

  Scenario: 經 Gateway 轉送後原始 authority、路徑、查詢、本文與簽章相關標頭未被改動
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務經 Gateway 提交帶有簽章相關標頭的請求
    Then API 所見的 authority、原始路徑與查詢、本文雜湊與簽章相關標頭與送出時完全相同

  Scenario Outline: 受信 Gateway 通道送來不合法的 XFCC 時 API 回應明確 401
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 受信 Gateway 測試憑證直連 API 並帶上 "<情況>" 的轉送憑證標頭
    Then Gateway 後的 API 回應 401 且不接受請求

    Examples:
      | 情況               |
      | 無標頭             |
      | 非 PEM 內容        |
      | 損毀的 PEM         |
      | 缺少 Cert 欄位     |
      | 重複的標頭         |
      | 多個 Cert 項目     |
      | 缺少 Hash 欄位     |
      | Hash 與憑證不符    |
      | Cert 後附加其他欄位 |
      | 同一 Cert 內含兩張憑證 |
      | 非法的百分比編碼   |
      | Cert 值內含未編碼空白 |

  Scenario: Body 與標頭宣稱其他 Client 時仍以憑證驗證結果為準
    Given 呼叫服務 "partner-a" 已以其憑證取得 reference token
    When 該服務以同一憑證和 token 經 Gateway 提交宣稱自己是 "partner-b" 的合作廠商資料
    Then Gateway 後的 API 回應 202 並回報已驗證 Client 為 "partner-a"
