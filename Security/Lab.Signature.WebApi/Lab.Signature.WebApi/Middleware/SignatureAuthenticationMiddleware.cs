using System.Text.Json;
using Lab.Signature.WebApi.Services;

namespace Lab.Signature.WebApi.Middleware;

/// <summary>
/// 只保護 <c>/api/protected/*</c> 路徑的簽章驗證 Middleware，整合 Step 4（Repository）、
/// Step 5（<see cref="ISignatureValidationHandler"/>）、Step 6（<see cref="INonceStore"/>）。
/// 驗證順序固定為：
/// ① 是否為 protected path
/// → ② 便宜檢查：必要 Header 存在、QueryString、Timestamp 格式/範圍/視窗（<see cref="ISignatureValidationHandler.ValidateCheapChecks"/>，
///    完全不需要讀 body，讓缺 Header/格式錯/過期這類請求可以在讀 body 之前就被擋下，避免被用來做 DoS）
/// → ③ Body Size 上限檢查（Content-Length 先擋，讀取時也有上限，避免超大/未知長度的 body 被整包讀進記憶體）
/// → ④ EnableBuffering 讀取並 buffer body
/// → ⑤ 用 raw body bytes 算 hash（Step5 內部計算）
/// → ⑥ 依 ApiKey 查 Secret（Step5 內部透過 Step4 Repository）
/// → ⑦ 組 canonical string（Step5 內部）
/// → ⑧ HMAC 計算 + constant-time 比對（Step5 內部）
/// → ⑨ 通過後原子性登記 Nonce（Step6）
/// → ⑩ Request.Body.Position = 0，讓 Controller 仍可讀到 body。
/// </summary>
public class SignatureAuthenticationMiddleware
{
    private const string ProtectedPathPrefix = "/api/protected";

    // 教學 Lab 用不到大檔案，64KB 已足夠涵蓋示範用的 JSON body。
    private const long MaxBodyBytes = 64 * 1024;

    private readonly RequestDelegate _next;

    public SignatureAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISignatureValidationHandler validationHandler,
        INonceStore nonceStore)
    {
        // ① 只套用 /api/protected/*，其餘路徑（含 /swagger、/health）直接放行
        if (!IsProtectedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-Timestamp"].FirstOrDefault();
        var nonce = context.Request.Headers["X-Nonce"].FirstOrDefault();
        var signature = context.Request.Headers["X-Signature"].FirstOrDefault();
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;

        // ② 便宜檢查：Header 齊全 → QueryString → Timestamp 格式/範圍/視窗，完全不touch body，
        // 讓沒有合法簽章材料的請求可以用極低成本被拒絕（High 1 修法：避免任何請求都先付出讀 body 的代價）。
        var cheapCheckResult = validationHandler.ValidateCheapChecks(apiKey, timestamp, nonce, signature, queryString);
        if (cheapCheckResult.IsFailure)
        {
            await WriteFailureResponseAsync(context, cheapCheckResult.Error.Reason, cheapCheckResult.Error.Message);
            return;
        }

        // ③ Body Size 上限：Content-Length 有值且超過上限就直接拒絕，完全不用碰 body stream。
        if (context.Request.ContentLength is { } declaredContentLength && declaredContentLength > MaxBodyBytes)
        {
            await WriteFailureResponseAsync(
                context,
                SignatureValidationFailureReason.PayloadTooLarge,
                $"Request Body 不可超過 {MaxBodyBytes} bytes");
            return;
        }

        // ④ 啟用 buffering 並讀取 raw body bytes，讀完立刻 rewind，
        // 之後不論驗證成功或失敗都不影響 Controller（失敗會直接 401 短路，不會呼叫 next）。
        // CopyWithLimitAsync 對實際讀取的位元組數也設有上限，防止 Content-Length 缺失或不實
        // （例如 chunked transfer）時仍把超大 body 整包讀進記憶體。
        context.Request.EnableBuffering();
        byte[] bodyBytes;
        await using (var memoryStream = new MemoryStream())
        {
            var exceededLimit = await CopyWithLimitAsync(
                context.Request.Body, memoryStream, MaxBodyBytes, context.RequestAborted);
            if (exceededLimit)
            {
                await WriteFailureResponseAsync(
                    context,
                    SignatureValidationFailureReason.PayloadTooLarge,
                    $"Request Body 不可超過 {MaxBodyBytes} bytes");
                return;
            }

            bodyBytes = memoryStream.ToArray();
        }

        context.Request.Body.Position = 0;

        // ⑤⑥⑦⑧：用 raw body bytes 算 hash → 依 ApiKey 查詢 → Canonical String → HMAC 比對，
        // 全部委由 Step5 的 SignatureValidationHandler 依固定順序驗證，這裡不重複實作驗證邏輯。
        var validationRequest = new SignatureValidationRequest(
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            queryString,
            apiKey,
            timestamp,
            nonce,
            signature,
            bodyBytes);

        var validationResult = await validationHandler.ValidateAsync(validationRequest, context.RequestAborted);
        if (validationResult.IsFailure)
        {
            await WriteFailureResponseAsync(context, validationResult.Error.Reason, validationResult.Error.Message);
            return;
        }

        // ⑨ 簽章驗證通過之後，才原子性登記 Nonce，避免無效請求也能消耗/污染 Nonce 空間
        var nonceConsumed = await nonceStore.TryConsumeAsync(apiKey!, nonce!, context.RequestAborted);
        if (!nonceConsumed)
        {
            await WriteFailureResponseAsync(
                context,
                SignatureValidationFailureReason.NonceReused,
                "Nonce 已經被使用過（可能是 Replay 攻擊）");
            return;
        }

        // ⑩ 讓後續 Controller 仍能從頭讀取 request body
        context.Request.Body.Position = 0;
        await _next(context);
    }

    private static bool IsProtectedPath(PathString path) =>
        path.StartsWithSegments(ProtectedPathPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 把 <paramref name="source"/> 複製到 <paramref name="destination"/>，但一旦累計讀到的位元組數超過
    /// <paramref name="limit"/> 就立刻停止並回傳 true（代表超過上限），呼叫端應視為 PayloadTooLarge，
    /// 不應該把已讀到的內容當作合法 body 使用。這是 Content-Length 檢查之外的第二道防線，
    /// 用來擋住 Content-Length 缺失或不實（例如 chunked transfer）的情況。
    /// </summary>
    private static async Task<bool> CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long limit,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        long totalBytesRead = 0;

        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytesRead += bytesRead;
            if (totalBytesRead > limit)
            {
                return true;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return false;
    }

    private static async Task WriteFailureResponseAsync(
        HttpContext context,
        SignatureValidationFailureReason reason,
        string message)
    {
        // ⚠️ Lab 教學模式：直接回傳具體失敗原因，方便教學展示與 Client Demo 顯示對應攻擊結果。
        // 正式環境應回泛化的 401 訊息（例如統一回 "Unauthorized"），詳細原因只寫 server log，
        // 避免把驗證細節洩漏給攻擊者用來窮舉/微調攻擊 payload。
        // Body 僅包含 reason/message，不含 Secret 或任何 HMAC 中間計算值。
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var payload = new { reason = reason.ToString(), message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
