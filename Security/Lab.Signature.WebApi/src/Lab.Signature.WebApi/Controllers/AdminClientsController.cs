using Lab.Signature.WebApi.Models;
using Lab.Signature.WebApi.Repositories;
using Lab.Signature.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab.Signature.WebApi.Controllers;

/// <summary>
/// 內部管理工具：核發（Create）與查詢（List）<see cref="ApiKeyClient"/>。
/// 路由本身在所有環境都會被 <c>MapControllers()</c> 註冊，實際的「非 Development 環境不可用」
/// 是由 <see cref="Middleware.AdminAuthenticationMiddleware"/> 在請求進入這個 Controller 前
/// 就攔截並回 404 達成的，不是靠條件式註冊路由。
/// 這不是對外自助申請 Key 的功能，也不套用簽章保護機制。
/// </summary>
[ApiController]
[Route("api/admin/clients")]
public class AdminClientsController : ControllerBase
{
    // ApiKey 主鍵衝突機率極低（16 random bytes），重試次數只是防禦性上限。
    private const int MaxCreateAttempts = 5;

    private readonly IApiKeyClientRepository _repository;
    private readonly IApiKeyGenerator _apiKeyGenerator;
    private readonly TimeProvider _timeProvider;

    public AdminClientsController(
        IApiKeyClientRepository repository,
        IApiKeyGenerator apiKeyGenerator,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _apiKeyGenerator = apiKeyGenerator;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 核發一組新的 Client 憑證。ClientName 不限唯一性，同一夥伴可有多把 Key。
    /// Secret 只在這次回應顯示明碼，之後無法再用 API 撈出。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateAdminClientRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var client = new ApiKeyClient
            {
                ApiKey = _apiKeyGenerator.GenerateApiKey(),
                Secret = _apiKeyGenerator.GenerateSecret(),
                ClientName = request.ClientName,
                CreatedAt = _timeProvider.GetUtcNow()
            };

            try
            {
                await _repository.AddAsync(client, cancellationToken);
                return Ok(new
                {
                    client.ApiKey,
                    client.Secret,
                    client.ClientName,
                    client.CreatedAt
                });
            }
            catch (DbUpdateException ex) when (attempt < MaxCreateAttempts && IsApiKeyUniqueViolation(ex))
            {
                // 只吞掉「確定是 ApiKey 主鍵/唯一鍵衝突」的例外並重新產生一組 ApiKey/Secret 再試一次。
                // 其他原因造成的 DbUpdateException（例如 ClientName 超過欄位長度限制）不應該被當成
                // ApiKey 衝突吞掉重試，讓它往上拋出正確的錯誤，而不是誤導成「無法產生唯一的 ApiKey」。
            }
        }

        return Conflict(new { message = "無法產生唯一的 ApiKey，請重試" });
    }

    /// <summary>
    /// 判斷 <see cref="DbUpdateException"/> 是否確實是 Postgres 的 unique_violation（SqlState 23505），
    /// 而不是其他原因（欄位長度超限、連線問題等）造成的寫入失敗。
    /// </summary>
    private static bool IsApiKeyUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;

    /// <summary>查詢所有已核發的 Client 清單，刻意不含 Secret 欄位。</summary>
    [HttpGet]
    public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
    {
        var clients = await _repository.GetAllAsync(cancellationToken);
        var response = clients.Select(c => new
        {
            c.ApiKey,
            c.ClientName,
            c.CreatedAt
        });

        return Ok(response);
    }
}

public record CreateAdminClientRequest(string ClientName);
