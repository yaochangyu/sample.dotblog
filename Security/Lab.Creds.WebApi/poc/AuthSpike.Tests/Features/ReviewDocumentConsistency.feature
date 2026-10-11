Feature: 審查意見的文件修正與現況一致
  依申請者驗證的審查意見：契約文件的註解、規格檔頭與 20 單的實作紀錄必須與現況一致；
  判斷題若為 lab 刻意例外，記錄於 ADR 0001 而不改程式。本檔以 @record 情境檢查文字，行為由既有情境保證。

  @record
  Scenario: OpenAPI 申請端點的驗證註解與 ADR 0001 一致
    Then 審查修正的 OpenAPI 不包含 "申請端點不需驗證"
    And 審查修正的 OpenAPI 包含 "申請端點須經已登錄申請者的 mTLS 驗證"

  @record
  Scenario: 規格檔頭不再宣稱尚未實作
    Then 審查修正的規格檔頭不包含 "尚未實作"

  @record
  Scenario: 20 單的 TDD 宣稱更正為實際情形
    Then 審查修正的 20 單不包含 "先看到失敗再實作"
    And 審查修正的 20 單包含 "行為 Scenario 未先見紅燈"

  @record
  Scenario: ADR 0001 記錄審查判斷題的刻意例外與待確認項目
    Then 審查修正的 ADR 0001 包含 "actorStatus 只區分"
    And 審查修正的 ADR 0001 包含 "CountPending"
    And 審查修正的 ADR 0001 包含 "AfterScenario"
    And 審查修正的 ADR 0001 包含 "步驟前綴"
    And 審查修正的 ADR 0001 包含 "nonce"
