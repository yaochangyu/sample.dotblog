# 受保護 API 驗證流程

## 驗證 Key 流程圖

下圖依照 `SignatureAuthenticationMiddleware` 與 `SignatureValidationHandler` 的實際順序，
說明一次受保護 API 請求如何被檢查。教學 Lab 為了方便觀察結果，失敗回應會帶出具體原因；
正式環境應考慮對外統一回覆一般性的未授權訊息，詳細原因只寫入受控的伺服器紀錄。

必要術語：

- **HMAC**：使用合作夥伴的 `Secret`，把請求內容算成一段只有持有相同 Secret 的雙方才能重現的簽章。
- **Nonce**：每次請求使用的一次性識別值，避免同一個請求被重送。
- **Canonical String**：伺服器與呼叫端共同約定的固定文字格式，內容包含 HTTP 方法、路徑、
  Timestamp、Nonce、ApiKey 與 Body 的 SHA-256 雜湊，雙方必須用完全相同的內容計算 HMAC。

```mermaid
flowchart TD
    A([Client 呼叫受保護 API]) --> B{四個必要 Header 是否齊全?}
    B -- 否 --> B1[401 HeaderMissing]
    B -- 是 --> C{是否帶 Query String?}
    C -- 是 --> C1[401 QueryStringNotAllowed]
    C -- 否 --> D{Timestamp 格式、範圍與時間視窗是否合法?}
    D -- 格式或數值不合法 --> D1[401 TimestampInvalidFormat]
    D -- 超過前後 5 分鐘 --> D2[401 TimestampExpired]
    D -- 合法 --> E{Content-Length 是否超過 64KB?}
    E -- 是 --> E1[401 PayloadTooLarge]
    E -- 否或未提供 --> F[讀取原始 Body，並檢查實際大小]
    F --> G{實際 Body 是否超過 64KB?}
    G -- 是 --> G1[401 PayloadTooLarge]
    G -- 否 --> H{ApiKey 是否存在於資料庫?}
    H -- 否 --> H1[401 ApiKeyNotFound]
    H -- 是 --> I[取得對應 Secret，組成 Canonical String]
    I --> J{HMAC 簽章是否比對成功?}
    J -- 否 --> J1[401 SignatureMismatch]
    J -- 是 --> K{Nonce 是否已被使用過?}
    K -- 是 --> K1[401 NonceReused]
    K -- 否 --> L[標記 Nonce 已使用]
    L --> M([通過驗證，交給受保護 API Controller])
```

## 驗證 Key 循序圖

以下循序圖展示一次成功呼叫的互動順序。Client 送出的 Body 必須維持原始內容，因為伺服器會
用原始 Body 計算雜湊；驗證成功後，Middleware 才會把請求交給 `OrdersController`。

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as SignatureAuthenticationMiddleware
    participant Repository as ApiKeyClientRepository
    participant NonceStore as INonceStore
    participant Controller as OrdersController

    Client->>Middleware: 送出受保護 API 請求<br/>X-Api-Key / X-Timestamp / X-Nonce / X-Signature
    Middleware->>Middleware: 便宜檢查 Header、Query String、Timestamp
    Middleware->>Middleware: 檢查 Content-Length 與實際 Body 大小
    Middleware->>Repository: 以 ApiKey 查詢對應的 Secret
    Repository-->>Middleware: 回傳 ApiKeyClient 與 Secret
    Middleware->>Middleware: 計算 Body SHA-256，組成 Canonical String
    Middleware->>Middleware: 使用 Secret 計算 HMAC，與 X-Signature 比對
    Middleware->>NonceStore: 以 ApiKey + Nonce 嘗試消費一次性 Nonce
    NonceStore-->>Middleware: 回傳成功，Nonce 尚未使用
    Middleware->>Controller: 驗證通過，轉交原始請求 Body
    Controller-->>Middleware: 處理訂單並回傳 API 結果
    Middleware-->>Client: 回傳成功的 API 回應
```

如果其中任一步驟失敗，Middleware 會直接回傳 HTTP 401，不會呼叫 `OrdersController`。例如：
缺少必要 Header 會是 `HeaderMissing`，簽章內容不一致會是 `SignatureMismatch`，同一個 Nonce
再次使用會是 `NonceReused`。
