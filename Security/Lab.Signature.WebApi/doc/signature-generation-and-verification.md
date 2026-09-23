# Client 產生與 Server 驗證簽章

這個機制使用的是 **HMAC-SHA256 簽章**，不是資料還原機制。Client 與 Server 各自獨立計算
HMAC：Client 產生 `X-Signature`，Server 使用資料庫中該 `ApiKey` 對應的 `Secret`，
依完全相同的規則重新計算，再用 constant-time compare 比對兩個結果。Server 不會把資料
還原成另一份簽章結果。

雙方能否得到相同結果，關鍵在於 `Canonical String` 必須完全一致：六個欄位的順序固定，
並且使用 `\n` 換行分隔。Body Hash 也必須針對相同的原始 Body bytes 計算；GET 或空 Body
使用空字串的 SHA-256。

```mermaid
flowchart TD
    subgraph CLIENT["Client 端：簽章產生"]
        C1[收集 HTTP Method、Path、Timestamp<br/>Unix epoch seconds、Nonce、ApiKey]
        C2[計算 Body 的 SHA-256<br/>GET 或空 Body 使用空字串的 SHA-256]
        C3[組成 Canonical String<br/>METHOD\nPATH\nTIMESTAMP\nNONCE\nAPI_KEY\nBODY_SHA256_HEX]
        C4[使用雙方共享的 Secret<br/>計算 HMAC-SHA256並轉成小寫 hex]
        C5[得到 X-Signature<br/>加入 X-Api-Key、X-Timestamp、X-Nonce、X-Signature]
        C1 --> C2 --> C3 --> C4 --> C5
    end

    C5 --> R[送出 HTTP Request]

    subgraph SERVER["Server 端：簽章驗證"]
        S1[收到請求與四個 Header]
        S2[用相同的 Method、Path、Timestamp、Nonce、ApiKey<br/>及 Body SHA-256 組出自己的 Canonical String]
        S3[從資料庫取得該 ApiKey 對應的 Secret]
        S4[重新計算 HMAC-SHA256<br/>得到 Server 端簽章值]
        S5{constant-time compare<br/>Server 簽章 vs X-Signature}
        S6[驗證通過，繼續處理請求]
        S7[401 SignatureMismatch]
        S1 --> S2 --> S3 --> S4 --> S5
        S5 -- 比對成功 --> S6
        S5 -- 不一致 --> S7
    end

    R --> S1
    P[關鍵前提：兩端使用完全相同的 Canonical String 格式<br/>欄位順序固定，欄位之間使用 \n 換行] -.-> C3
    P -.-> S2
```

## 流程重點

- Client 與 Server 是**各自獨立計算** HMAC，Server 不會還原 Client 的簽章。
- 比對使用 `constant-time compare`，避免因比較時間差異造成 timing attack。
- 只有 HMAC 結果一致時才算驗證通過；不一致會回傳 `401 SignatureMismatch`。
