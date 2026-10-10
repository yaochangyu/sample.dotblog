Feature: 查證服務異常回應一律視為無法查證
  依 07 單 fail closed 原則，授權伺服器 introspection 回任何非預期的 5xx 或格式錯誤，
  業務 API 一律回應 503 verification_unavailable，不可誤判為憑證無效的 401。
  Token 明確為 inactive 才是 401。

  Scenario Outline: introspection 回應 <情境> 時回 503 而非 401
    Given 查證服務以假的授權伺服器取代，introspection 行為為 "<行為>"
    When 呼叫端持任意 Token 查詢訂單
    Then 查證故障情境的回應為 <狀態>

    Examples:
      | 情境        | 行為         | 狀態 |
      | 502         | status-502   | 503  |
      | 500         | status-500   | 503  |
      | 壞掉的 JSON | bad-json     | 503  |
      | 空內容      | empty        | 503  |
      | inactive    | inactive     | 401  |
