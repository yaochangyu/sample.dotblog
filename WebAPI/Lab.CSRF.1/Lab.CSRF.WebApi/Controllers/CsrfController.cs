using Lab.CSRF.WebApi.Filters;
using Lab.CSRF.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab.CSRF.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsrfController : ControllerBase
{
    private readonly ICsrfTokenService _csrfTokenService;
    private readonly ILogger<CsrfController> _logger;

    public CsrfController(ICsrfTokenService csrfTokenService, ILogger<CsrfController> logger)
    {
        _csrfTokenService = csrfTokenService;
        _logger = logger;
    }

    /// <summary>
    /// 取得 CSRF Token
    /// </summary>
    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var token = _csrfTokenService.GenerateToken();

        _logger.LogInformation("產生 CSRF Token 給客戶端");

        return Ok(new
        {
            token,
            expiresIn = 1800 // 30 分鐘
        });
    }

    /// <summary>
    /// 測試需要 CSRF Token 的端點
    /// </summary>
    [HttpPost("test")]
    [ValidateCsrfToken]
    public IActionResult TestProtectedEndpoint([FromBody] TestRequest request)
    {
        _logger.LogInformation("受保護的端點被呼叫: {Message}", request.Message);

        return Ok(new
        {
            success = true,
            message = "CSRF 驗證通過！",
            receivedData = request.Message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 測試需要 CSRF Token 的 PUT 端點
    /// </summary>
    [HttpPut("update/{id}")]
    [ValidateCsrfToken]
    public IActionResult UpdateData(int id, [FromBody] TestRequest request)
    {
        _logger.LogInformation("更新資料 ID: {Id}, Message: {Message}", id, request.Message);

        return Ok(new
        {
            success = true,
            message = "資料更新成功！",
            id,
            updatedData = request.Message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 測試需要 CSRF Token 的 DELETE 端點
    /// </summary>
    [HttpDelete("delete/{id}")]
    [ValidateCsrfToken]
    public IActionResult DeleteData(int id)
    {
        _logger.LogInformation("刪除資料 ID: {Id}", id);

        return Ok(new
        {
            success = true,
            message = "資料刪除成功！",
            deletedId = id,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 測試不需要 CSRF Token 的公開端點
    /// </summary>
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        _logger.LogInformation("公開端點被呼叫");

        return Ok(new
        {
            message = "這是公開端點，不需要 CSRF Token",
            timestamp = DateTime.UtcNow
        });
    }
}

public record TestRequest(string Message);
