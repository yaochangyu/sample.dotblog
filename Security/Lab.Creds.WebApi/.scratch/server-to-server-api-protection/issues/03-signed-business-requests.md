# 03: 業務 API 驗證原始呼叫端請求簽章

**What to build:** 呼叫服務簽署示範業務請求，最終 API 能辨認原始呼叫端並拒絕被竄改或混用身分的請求，合法簽章請求維持正常回應。

**Blocked by:** 02 — 呼叫服務取得 Token 並經可信入口呼叫 API。

**Status:** ready-for-agent

- [x] 取得共同 HTTP Message Signatures 規則與演算法的確認，明訂必要欄位、Token 綁定資訊表示及中介改寫處理（已確認採用 RFC 9421 Lab Profile v1，規格見 `spec.md`〈請求簽章與防重放〉）。
- [ ] 所有示範業務 API 呼叫必須簽章；無 Body 請求仍驗證方法、目標與必要欄位。
- [ ] 有 Body 時簽章涵蓋摘要與必要內容型別，最終 API 核對摘要和收到的實際 Body 相符。
- [ ] 涵蓋完整業務目標、會影響語意的查詢參數、必要標頭、適用時的 Idempotency Key 及 Token 綁定資訊。
- [ ] 簽章使用不同於 mTLS 的私鑰，並確認簽章金鑰屬於已驗證 Token 識別的同一 Client。
- [ ] 缺少簽章、內容或目標遭竄改、替換授權脈絡、混用不同 Client 合法金鑰時拒絕。
- [ ] Gateway 不破壞簽章驗證脈絡；若有改寫，以已確認契約保留可驗證的原始資訊，不信任外部自稱的原始欄位。
- [ ] 使用實際簽署與驗證的整合情境證明合法請求可通過及竄改請求被拒絕，不宣稱 nonce 防重放已完成。

對應驗收：AC-03 的簽章金鑰部分、AC-07。

### 已確認之簽章規格摘要（RFC 9421 Lab Profile v1）
- **演算法與格式**：RFC 9421 IANA `ecdsa-p256-sha256`（IEEE P1363 `r || s` 64-byte 格式，.NET 10 `ECDsa.SignData/VerifyData` 預設）；每 Client / 各環境獨立 P-256 金鑰對，與 mTLS RSA-2048 分開；`keyid` 靜態登記，且所屬 `client_id` 須與 Token 一致，禁止動態演算法協商。
- **標頭與參數**：單一標籤 `sig1`；`@signature-params` 固定順序 `;created=<int>;expires=<int>;nonce="<str>";keyid="<str>";alg="ecdsa-p256-sha256"`。
- **覆蓋元件順序**：
  - 讀取無 Body（GET/HEAD）：`"@method" "@authority" "@path" "@query" "authorization"`（無 query 時值為 `?`；直接簽署原始 `Authorization` 標頭值，禁止自訂 token-hash 標頭；日誌禁止輸出 Token、Authorization、signature base 或簽章值）。
  - 有副作用無 Body（DELETE 無 Body）：`"@method" "@authority" "@path" "@query" "authorization" "idempotency-key"`。
  - 有副作用有 Body（POST/PUT/PATCH，或 DELETE 有 Body）：`"@method" "@authority" "@path" "@query" "authorization" "content-type" "content-digest" "idempotency-key"`。
- **Body 與摘要**：依實際接收 bytes 判斷（不單靠 `Content-Length`）；有 Body 必加 `content-digest`（RFC 9530 `sha-256=:<標準 base64>:` 逐 byte 比對）與 `content-type`。
- **時間與 Nonce**：`expires = created + 60`；驗證條件 `expires > created`、`expires - created <= 60`、`created - 30 <= now <= expires + 30`（容差 30 秒，最大接受區間 120 秒）；nonce 採 CSPRNG 16 bytes 無 padding base64url（22 字元），每 attempt 新值；03 僅驗格式與時間窗，不驗唯一性（防重放由 04 實作）。
- **驗證邊界**：RFC B.2.4 測試向量與 Envoy 標頭逐字轉送保留至 03 實作實測；不宣稱已完成防重放；05 資料範圍授權來源未定。完整契約詳見 `spec.md`。
