# language: zh-TW
功能: 簽章保護 API 端點
  身為合作夥伴，我呼叫 /api/protected/* 端點時必須帶上正確的 HMAC-SHA256 簽章，
  Middleware 才會放行；任何竄改、重放、過期或格式錯誤都應該被正確擋下並回傳具體原因。

  場景: 正常 GET 請求應該驗證成功
    假設 我已經在系統中註冊了一個新的 ApiKey
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200

  場景: 正常 POST 請求應該驗證成功，且 Controller 應該讀到正確的 Body
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Body 是:
      """
      {"productName":"Gadget","amount":250}
      """
    當 我送出 POST /api/protected/orders 請求
    那麼 回應狀態碼應該是 200
    而且 回應內容應該包含 productName "Gadget" 與 amount 250

  場景: Body 被竄改但沿用舊簽章應該被拒絕
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Body 是:
      """
      {"productName":"Widget","amount":50}
      """
    而且 我在送出前把 Body 竄改成:
      """
      {"productName":"Widget","amount":999999}
      """
    當 我送出 POST /api/protected/orders 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "SignatureMismatch"

  場景: 重送相同的 Nonce 應該被視為 Replay 攻擊
    假設 我已經在系統中註冊了一個新的 ApiKey
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200
    當 我沿用上一次的 Timestamp 與 Nonce 重新送出相同請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "NonceReused"

  場景大綱: Timestamp 超出 ±5 分鐘視窗時應該被拒絕
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Timestamp 是 <Minutes> 分鐘<Direction>
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "TimestampExpired"

    例子:
      | Minutes | Direction |
      | 6       | 前         |
      | 6       | 後         |

  場景: 使用不存在的 ApiKey 應該被拒絕
    假設 我使用一個從未註冊過的 ApiKey
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "ApiKeyNotFound"

  場景大綱: 缺少任一必要 Header 時應該回傳 401 HeaderMissing
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Body 是:
      """
      {"productName":"Widget","amount":50}
      """
    而且 我沒有帶 "<Header>" 這個 Header
    當 我送出 POST /api/protected/orders 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "HeaderMissing"

    例子:
      | Header      |
      | X-Api-Key   |
      | X-Timestamp |
      | X-Nonce     |
      | X-Signature |

  場景: Timestamp 格式錯誤導致驗證失敗
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Timestamp 是格式錯誤的字串 "not-a-unix-timestamp"
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "TimestampInvalidFormat"

  場景: 帶 QueryString 的請求應該被直接拒絕
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 QueryString 帶有 "?foo=bar"
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "QueryStringNotAllowed"

  場景: GET 請求使用空 Body 的雜湊值仍然能驗證成功
    假設 我已經在系統中註冊了一個新的 ApiKey
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200

  場景: Signature 使用大寫 hex 仍然通過驗證
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Signature 故意轉成大寫
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200

  場景: Signature 包含非法字元（非 hex）導致驗證失敗
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Signature 故意換成不合法的非 hex 字串
    當 我送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "SignatureMismatch"

  場景: 同一個 Nonce 值搭配不同的 ApiKey 應該互不影響
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 我已經在系統中額外註冊了另一個 ApiKey
    而且 Nonce 固定為 "shared-nonce-across-clients"
    當 我使用第一組 ApiKey 送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200
    當 我使用第二組 ApiKey 送出 GET /api/protected/orders/{id} 請求
    那麼 回應狀態碼應該是 200

  場景: 併發送出同一個 Nonce，只有一個請求應該成功
    假設 我已經在系統中註冊了一個新的 ApiKey
    當 我同時送出 10 個使用相同 Nonce 的並發請求
    那麼 剛好有 1 個請求成功、其餘 9 個請求都回傳 401 reason "NonceReused"

  場景: 簽章驗證失敗時的回應不應該包含 Secret 或 HMAC 中間值
    假設 我已經在系統中註冊了一個新的 ApiKey
    而且 Body 是:
      """
      {"productName":"Widget","amount":50}
      """
    而且 我在送出前把 Body 竄改成:
      """
      {"productName":"Widget","amount":999999}
      """
    當 我送出 POST /api/protected/orders 請求
    那麼 回應狀態碼應該是 401
    而且 回應內容應該包含 reason "SignatureMismatch"
    而且 回應內容不應該包含 Secret 字串
    而且 回應內容不應該包含任何 HMAC 中間計算值
