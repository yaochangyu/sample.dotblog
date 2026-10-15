@real-storage
Feature: 授權伺服器資料庫與管理操作的交易一致性
  依 25 單驗收項目，OpenIddict、信任名單、登錄申請、持有證明 Nonce 與管理操作稽核改存至 PostgreSQL 資料庫。
  AC-36: 管理操作套用失敗時回滾並留下一筆 failed 稽核；若 failed 稽核也寫不進去回報 503 audit_unavailable。
  AC-37: 兩位管理員或兩個授權伺服器執行個體同時核准/拒絕同一筆申請，以 FOR UPDATE 保證僅一個決定生效，另一個得到 409。
  AC-38: 同一 Client 併發提交申請，鎖定 Client 計算 pending 數量，不超過上限（超過 429）。
  AC-39: 同一份持有證明 Nonce 併發送出兩次，保證只建立一筆申請，另一次被拒絕且不佔用額度。
  AC-40: 授權伺服器重啟或由另一個執行個體接手後，信任名單、申請、稽核與核發之 Token 依然有效且一致。

  Background:
    Given 真實儲存測試環境已就緒且授權伺服器連線至 "auth-db"

  Scenario: 管理操作套用失敗時信任名單不變並留下獨立交易的 failed 稽核（AC-36）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    When 授權伺服器模擬管理操作套用失敗
    And 管理員嘗試核准該申請
    Then 管理操作失敗且未變更憑證狀態為核准
    And 資料庫稽核紀錄未留下該操作的 accepted 紀錄
    And 資料庫稽核紀錄留下一筆 failed 稽核
    And 呼叫端無法以該未核准的憑證取得存取 Token
    And 呼叫端仍可以原憑證成功取得存取 Token

  Scenario: 簽章金鑰管理操作套用失敗時回滾並留下獨立交易的 failed 稽核（AC-36）
    Given 呼叫端 "orders-client" 提出簽章金鑰登錄申請
    When 授權伺服器模擬簽章金鑰管理操作套用失敗
    And 管理員嘗試核准該簽章金鑰申請
    Then 管理操作失敗且未變更簽章金鑰狀態為核准
    And 資料庫稽核紀錄未留下該操作的 accepted 紀錄
    And 資料庫稽核紀錄留下一筆 failed 稽核

  Scenario: 管理操作套用失敗且 failed 稽核無法寫入時回報 503 audit_unavailable（AC-36）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    When 授權伺服器模擬管理操作套用失敗且稽核寫入失敗
    And 管理員嘗試核准該申請
    Then 管理操作回傳 503 且錯誤原因為 "audit_unavailable"
    And 呼叫端無法以該未核准的憑證取得存取 Token
    And 呼叫端仍可以原憑證成功取得存取 Token

  Scenario: 兩個執行個體併發核准同一筆申請保證僅一個成功（AC-37）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    When 兩個管理員執行個體併發核准該申請
    Then 一個執行個體回應核准成功且另一個執行個體回應 409
    And 該申請在資料庫中的最終狀態為 "approved"

  Scenario: 兩個執行個體併發核准與拒絕同一筆申請保證僅一個生效且另一個回應 409（AC-37）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    When 兩個管理員執行個體併發分別核准與拒絕該申請
    Then 一個執行個體回應成功且另一個執行個體回應 409
    And 該申請在資料庫中的最終狀態不為 "pending"

  Scenario: 同一 Client 併發提交多筆申請受限於待核准上限（AC-38）
    When 呼叫端 "orders-client" 併發送出 10 筆憑證登錄申請
    Then 剛好 5 筆申請獲得接受且其餘 5 筆回應 429
    And 呼叫端 "orders-client" 在資料庫中的待核准申請數為 5

  Scenario: 同一份持有證明併發送出兩次僅建立一筆且不佔額度（AC-39）
    When 呼叫端 "orders-client" 以相同持有證明 nonce 併發送出 2 次申請
    Then 剛好 1 筆申請獲得接受且另 1 筆被拒絕
    And 呼叫端 "orders-client" 在資料庫中的待核准申請數為 1

  Scenario: 授權伺服器重啟後信任名單與 Token 驗證依然有效且一致（AC-40）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    And 管理員核准該申請
    And 呼叫端以核准的憑證取得存取 Token
    When 授權伺服器以原資料庫重新啟動
    Then 重啟前核發的存取 Token 依然有效
    And 核准的憑證依然能成功取得 Token
    And 資料庫中該申請的狀態依然為 "approved"
    And 管理信任名單中包含該核准的憑證指紋
    And 重啟前留下的管理稽核紀錄依然存在且一致
    And 資料庫中已登錄的管理員依然能成功讀取信任名單
    And 未登錄於資料庫的自簽憑證無法存取管理介面

  Scenario: 授權伺服器由另一個執行個體接手後信任名單與 Token 驗證依然有效且一致（AC-40）
    Given 呼叫端 "orders-client" 提出憑證登錄申請
    And 管理員核准該申請
    And 呼叫端以核准的憑證取得存取 Token
    When 由另一個授權伺服器執行個體接手連線至原資料庫
    Then 重啟前核發的存取 Token 依然有效
    And 核准的憑證依然能成功取得 Token
    And 資料庫中該申請的狀態依然為 "approved"
    And 管理信任名單中包含該核准的憑證指紋
    And 重啟前留下的管理稽核紀錄依然存在且一致
    And 資料庫中已登錄的管理員依然能成功讀取信任名單
    And 未登錄於資料庫的自簽憑證無法存取管理介面

  Scenario: 授權伺服器重啟後簽章金鑰登錄與信任名單依然有效且一致（AC-40）
    Given 呼叫端 "orders-client" 提出簽章金鑰登錄申請
    And 管理員核准該簽章金鑰申請
    When 授權伺服器以原資料庫重新啟動
    Then 資料庫中該簽章金鑰申請的狀態依然為 "approved"
    And 管理信任名單中包含該核准的簽章金鑰
    And 重啟前留下的管理稽核紀錄依然存在且一致
