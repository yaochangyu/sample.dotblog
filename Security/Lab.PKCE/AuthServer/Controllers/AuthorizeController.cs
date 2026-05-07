using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorizeController(
    AuthorizationCodeStore store,
    UserStore users,
    SessionStore sessions) : ControllerBase
{
    private const string SessionCookieName = "sid";

    [HttpPost]
    public IActionResult Post([FromBody] AuthorizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodeChallenge))
            return BadRequest("code_challenge 不可為空");

        if (request.CodeChallengeMethod != "S256")
            return BadRequest("僅支援 S256");

        string username;

        // 先查 Cookie，有效 Session 直接跳過帳密輸入
        if (TryGetValidSession(out var session))
        {
            username = session!.Username;
        }
        else
        {
            // 沒有 Session 才驗帳密
            if (!users.Validate(request.Username, request.Password))
                return Unauthorized("帳號或密碼錯誤");

            username = request.Username;

            // 建立 Session 並寫入 HttpOnly Cookie
            var newSession = sessions.Create(username);
            Response.Cookies.Append(SessionCookieName, newSession.SessionId, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires  = newSession.ExpiresAt
            });
        }

        var code = store.Save(new AuthorizationCode
        {
            CodeChallenge       = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Username            = username
        });

        return Ok(new { code, username });
    }

    [HttpGet("session")]
    public IActionResult GetSession()
    {
        if (!TryGetValidSession(out var session))
            return Unauthorized("Session 不存在或已過期");

        return Ok(new { username = session!.Username });
    }

    private bool TryGetValidSession(out Session? session)
    {
        session = null;
        if (!Request.Cookies.TryGetValue(SessionCookieName, out var sid))
            return false;

        session = sessions.Get(sid!);
        if (session is null || session.IsExpired)
        {
            // 過期 Session 順手清掉
            if (session is not null) sessions.Remove(sid!);
            Response.Cookies.Delete(SessionCookieName);
            session = null;
            return false;
        }

        return true;
    }
}
