Feature: 選型決策與接入契約紀錄可追溯
  01 單的決策、候選核對、支援證據分級與未決事項，皆以可檢查的紀錄與實際設定對應。

  Background:
    Given 01 單的實作紀錄可讀取

  Scenario: 授權伺服器採使用者確認的 OpenIddict 候選
    Then 實作紀錄包含 "授權伺服器：OpenIddict"
    And 授權伺服器的 client_credentials 與 mTLS 用戶端認證由 OpenIddict 啟用
    And 授權伺服器未使用 Client Secret

  Scenario: OpenIddict 候選的正式版本與發佈時間有紀錄
    Then 實作紀錄包含 "正式版本：7.7.1"
    And 實作紀錄包含 "發佈於 2026-09-17"
    And 實作紀錄包含 "非預覽版"

  Scenario: OpenIddict 候選的維護狀態有紀錄
    Then 實作紀錄包含 "2026 年內持續釋出"

  Scenario: OpenIddict 套件授權條件為 Apache-2.0
    Then OpenIddict 伺服器套件的授權條件為 "Apache-2.0"
    And 實作紀錄包含 "授權條件：Apache-2.0"

  Scenario: OpenIddict 候選與 ASP.NET Core 10 整合
    Then 實作紀錄包含 "net10.0 組件"
    And 專案參照 OpenIddict.AspNetCore 與 OpenIddict.Validation.SystemNetHttp 7.7.1

  Scenario: API 開發方式採 API First 且未引入 Code First 產生器
    Then 實作紀錄包含 "API 開發方式：API First"
    And 專案未引入 Code First 的 OpenAPI 產生套件

  Scenario: 首個示範業務操作為建立訂單
    Then 實作紀錄包含 "首個示範業務操作：建立訂單"
    And 建立訂單契約檔以 POST /orders 定義 createOrder

  Scenario: 證據依支援等級分類
    Then 實作紀錄包含 "已實際驗證（BDD 通過）"
    And 實作紀錄包含 "需整合、尚未驗證成功"
    And 實作紀錄包含 "未驗證"

  Scenario: PKI 用戶端認證不被宣稱為已支援
    Then 實作紀錄包含 "本 spike 不宣稱支援"

  Scenario: 證據不以 dev 文件作為可用性證明
    Then 實作紀錄包含 "僅用於定位用法"
    And 實作紀錄包含 "可用性以上述實際執行為準"

  Scenario: 驗證方式可重現
    Then 實作紀錄包含 "dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj"

  Scenario: 已確認契約與未決事項有紀錄
    Then 實作紀錄包含 "已確認的契約（spike 範圍）"
    And 實作紀錄包含 "未決事項與阻擋"

  Scenario: Gateway 項目標註為本 lab 不實作且不阻擋
    Then 實作紀錄包含 "Gateway 候選與信任契約（本 lab 不實作，略過，不阻擋）"
    And 01 單 Gateway 驗收項目已勾選

  Scenario: 本 lab 不經 Gateway，業務 API 直接以 TLS 連線憑證驗證
    Given 已登錄服務 "orders-client" 與其用戶端憑證
    And 呼叫端已以 "orders-client" 憑證取得 Token
    When 持有 Token 的呼叫端以 "orders-client" 送出建立訂單請求
    Then 建立訂單 API 回應 201

  @ignore
  Scenario: Gateway 終止 mTLS 並以信任契約轉送（本 lab 不實作）
    Given 可信 Gateway 終止 mTLS 並轉送呼叫端憑證資訊
    When 呼叫端繞過 Gateway 直接送出建立訂單請求
    Then 業務 API 拒絕不可信的入口標頭
