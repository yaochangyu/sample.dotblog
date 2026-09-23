using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lab.Signature.WebApi.Middleware;

/// <summary>
/// 保護 <c>/api/admin/*</c> 路徑的內部管理端點。<c>AdminClientsController</c> 的路由在所有環境都會被
/// <c>MapControllers()</c> 註冊，「非 Development 環境不可用」不是靠條件式註冊路由，而是完全由這個
/// Middleware 在請求進入 Controller 前攔截達成：
/// ① 非 <see cref="IWebHostEnvironment.IsDevelopment"/> 環境一律回 404 —— 對外表現得像端點完全不存在，
///    而不是回 401/403（避免洩漏「這個路徑存在但被擋下」的資訊）。
/// ② Development 環境下，比對 Request Header <c>X-Admin-Key</c> 與設定值 <c>AdminApiKey</c>
///    （來源 <c>appsettings.Development.json</c>），缺少或不符一律回 401。
/// 這不是對外的簽章保護機制，不套用 <see cref="SignatureAuthenticationMiddleware"/> 的流程。
/// </summary>
public class AdminAuthenticationMiddleware
{
    private const string AdminPathPrefix = "/api/admin";
    private const string AdminKeyHeader = "X-Admin-Key";
    private const string AdminApiKeyConfigKey = "AdminApiKey";

    private readonly RequestDelegate _next;

    public AdminAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IWebHostEnvironment environment, IConfiguration configuration)
    {
        if (!IsAdminPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // ① 非 Development 環境：路由本身仍有註冊，但在這裡直接短路回 404，
        // 讓 Admin API 對外表現得像完全不存在。
        if (!environment.IsDevelopment())
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // ② Development 環境：比對 X-Admin-Key。Admin Key 不是對外簽章材料，一般字串比對即可，
        // 但沿用 CryptographicOperations.FixedTimeEquals 作為防禦性寫法，與簽章驗證風格一致。
        var providedKey = context.Request.Headers[AdminKeyHeader].FirstOrDefault();
        var expectedKey = configuration[AdminApiKeyConfigKey];

        if (string.IsNullOrEmpty(providedKey) || string.IsNullOrEmpty(expectedKey) || !IsKeyMatch(expectedKey, providedKey))
        {
            await WriteUnauthorizedResponseAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsAdminPath(PathString path) =>
        path.StartsWithSegments(AdminPathPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsKeyMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var payload = new { message = "Unauthorized" };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
