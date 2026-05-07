using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
public class TokenController(
    AuthorizationCodeStore store,
    PkceService pkce,
    AccessTokenStore tokens) : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] TokenRequest request)
    {
        if (request.GrantType != "authorization_code")
            return BadRequest("不支援的 grant_type");

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.CodeVerifier))
            return BadRequest("code 與 code_verifier 不可為空");

        var entry = store.TakeAndRemove(request.Code);
        if (entry is null)
            return BadRequest("authorization_code 不存在或已使用");

        if (entry.IsExpired)
            return BadRequest("authorization_code 已過期");

        if (!pkce.Verify(request.CodeVerifier, entry.CodeChallenge))
            return Unauthorized("PKCE 驗證失敗：verifier 與 challenge 不符");

        var token = $"eyJ.{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=')}.SIG";

        // 核發後存入 store，供 /api/me 驗證使用
        tokens.Save(token, entry.Username);

        return Ok(new TokenResponse { AccessToken = token });
    }
}
