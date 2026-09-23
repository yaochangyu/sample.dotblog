# API Key 申請流程

## 申請流程

| 步驟 | 負責角色 | 說明 |
| --- | --- | --- |
| 1. 提出需求 | 合作夥伴 | 填寫 API Key 申請單，說明使用單位、用途、端點與預計上線時間，送交內部窗口 |
| 2. 收件與審核 | 內部窗口／審核人 | 確認申請目的、使用範圍、聯絡資料與上線時程；需要補充資料時退回申請 |
| 3. 內部核發 | 管理員 | 審核通過後，在 Development 環境使用內部 Admin API `POST /api/admin/clients`，Request Body 只需提供 `ClientName` |
| 4. 記錄核發結果 | 管理員 | 保存核發的 `ApiKey`、合作夥伴名稱與核發時間；`Secret` 不應放入一般申請紀錄或 Log |
| 5. 安全交付 | 管理員／內部窗口 | `Secret` 只會在核發 API 的這次回應中顯示一次，必須透過核准的安全管道交付，不使用 Email 明碼寄送 |
| 6. 開始串接 | 合作夥伴 | 收到 `ApiKey` 與 `Secret` 後，依 API 規格產生四個必要 Header，開始呼叫受保護 API |

### 申請流程圖

下圖將人工審核流程與實際的 Admin API 呼叫放在同一條流程中。管理員呼叫
`POST /api/admin/clients` 時，必須帶 `X-Admin-Key` Header，且只在 Development
環境可用；非 Development 環境會回傳 404，缺少或錯誤的 Admin Key 會回傳 401。

```mermaid
flowchart TD
    A([合作夥伴提出需求]) --> B[填寫 API Key 申請單]
    B --> C[內部窗口收件與審核]
    C --> D{審核結果?}
    D -- 不通過 --> E[退回申請／結束]
    D -- 通過 --> F[通知管理員核發]
    F --> G{執行環境是 Development?}
    G -- 否 --> H[Admin API 回傳 404 Not Found／結束]
    G -- 是 --> I{X-Admin-Key 是否正確?}
    I -- 否 --> J[Admin API 回傳 401 Unauthorized／結束]
    I -- 是 --> K[管理員呼叫 POST /api/admin/clients<br/>Header: X-Admin-Key<br/>Body: ClientName]
    K --> L[AdminClientsController 寫入 ApiKeyClients]
    L --> M[回傳 ApiKey、Secret、ClientName、CreatedAt<br/>Secret 只在這次回應顯示一次]
    M --> N[管理員透過安全管道交付 ApiKey／Secret]
    N --> O([合作夥伴開始呼叫受保護 API])
```

### 申請流程循序圖

下圖說明申請單核准後，管理員如何實際呼叫 Admin API。Admin API 的 Request Body
只需帶 `ClientName`；成功回應包含 `ApiKey`、`Secret`、`ClientName` 與 `CreatedAt`，
其中 `Secret` 只有這次核發回應會顯示。

```mermaid
sequenceDiagram
    participant Partner as 廠商（合作夥伴）
    participant Reviewer as 內部窗口／審核人
    participant Admin as 管理員
    participant AdminApi as Admin API<br/>AdminClientsController<br/>POST /api/admin/clients
    participant Database as 資料庫<br/>ApiKeyClients

    Partner->>Reviewer: 送出 API Key 申請單
    Reviewer->>Reviewer: 檢查申請用途、端點與上線時間
    alt 審核不通過
        Reviewer-->>Partner: 退回申請並說明補件或不通過原因
    else 審核通過
        Reviewer->>Admin: 通知可核發 API Key
        Admin->>AdminApi: POST /api/admin/clients<br/>X-Admin-Key: Admin Key<br/>Body: {"clientName":"示範夥伴股份有限公司"}
        AdminApi->>AdminApi: AdminAuthenticationMiddleware 檢查 Development 與 X-Admin-Key
        alt 非 Development 環境
            AdminApi-->>Admin: 404 Not Found
        else 缺少或錯誤 X-Admin-Key
            AdminApi-->>Admin: 401 Unauthorized
        else 驗證通過
            AdminApi->>Database: 新增 ApiKeyClient
            Database-->>AdminApi: 儲存 ApiKey、Secret、ClientName、CreatedAt
            AdminApi-->>Admin: 200 回傳 ApiKey、Secret、ClientName、CreatedAt<br/>Secret 只在這次回應顯示一次
            Admin->>Partner: 透過安全管道交付 ApiKey／Secret
        end
    end
```

管理員核發時使用的 Admin API 是內部管理工具，不是合作夥伴可以直接使用的申請入口。此 API
需要 `X-Admin-Key`，且只有 Development 環境可用；非 Development 環境的請求會回傳 404。

合作夥伴收到的兩項資料用途不同：

- **ApiKey**：用來辨識是哪一個合作夥伴在呼叫 API。
- **Secret**：只有合作夥伴與 API 伺服器應知道，用來產生每次請求的 HMAC 簽章；不能放在瀏覽器、
  前端程式碼、申請單、Log 或公開版本庫。
