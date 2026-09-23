namespace Lab.Signature.WebApi.Services;

/// <summary>
/// 具體的簽章驗證失敗原因，供 Middleware（Step 7）決定要回傳的 401 訊息。
/// </summary>
public enum SignatureValidationFailureReason
{
    /// <summary>缺少 X-Api-Key / X-Timestamp / X-Nonce / X-Signature 其中之一。</summary>
    HeaderMissing,

    /// <summary>Protected 端點不支援 Query String。</summary>
    QueryStringNotAllowed,

    /// <summary>X-Timestamp 不是合法的 Unix epoch seconds 數字字串。</summary>
    TimestampInvalidFormat,

    /// <summary>Timestamp 超出 ±5 分鐘視窗。</summary>
    TimestampExpired,

    /// <summary>X-Api-Key 在資料庫中查無對應的 Client。</summary>
    ApiKeyNotFound,

    /// <summary>重新計算的簽章與 X-Signature 不一致（或格式非合法 hex）。</summary>
    SignatureMismatch,

    /// <summary>簽章驗證通過，但該 ApiKey+Nonce 組合已經被使用過（Step 6 NonceStore 判定為 Replay）。</summary>
    NonceReused,

    /// <summary>Request Body 超過允許的大小上限，Middleware 會在讀取/緩衝 body 前就拒絕，避免 DoS。</summary>
    PayloadTooLarge
}

/// <summary>
/// 簽章驗證失敗時的詳細資訊。訊息文字僅供教學模式使用，正式環境應改回泛化錯誤。
/// </summary>
public sealed record SignatureValidationFailure(SignatureValidationFailureReason Reason, string Message);

/// <summary>
/// 簽章驗證所需的原始請求資料，由呼叫端（未來的 Middleware）組裝並傳入。
/// </summary>
public sealed record SignatureValidationRequest(
    string Method,
    string Path,
    string? QueryString,
    string? ApiKey,
    string? Timestamp,
    string? Nonce,
    string? Signature,
    byte[] Body);
