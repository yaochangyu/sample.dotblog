using CSharpFunctionalExtensions;
using Lab.Signature.WebApi.Models;

namespace Lab.Signature.WebApi.Services;

public interface ISignatureValidationHandler
{
    /// <summary>
    /// 不需要讀取 request body 的「便宜檢查」：必要 Header 是否齊全、是否帶 QueryString、
    /// Timestamp 格式與 ±5 分鐘視窗。Middleware 應在啟用 body buffering 之前先呼叫這個方法，
    /// 只有通過之後才值得付出讀取/緩衝 body 的成本，避免匿名攻擊者用大 body 造成 DoS。
    /// </summary>
    UnitResult<SignatureValidationFailure> ValidateCheapChecks(
        string? apiKey,
        string? timestamp,
        string? nonce,
        string? signature,
        string? queryString);

    /// <summary>
    /// 驗證 Request 的簽章是否正確（含上述便宜檢查 + ApiKey 查詢 + Canonical String + HMAC 比對）。
    /// 此階段尚未含 Nonce 防重放檢查（見 Step 6），
    /// 成功時回傳對應的 <see cref="ApiKeyClient"/>，供後續流程（如 Nonce 登記）使用。
    /// </summary>
    Task<Result<ApiKeyClient, SignatureValidationFailure>> ValidateAsync(
        SignatureValidationRequest request,
        CancellationToken cancellationToken = default);
}
