using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LogoutController(SessionStore sessions) : ControllerBase
{
    private const string SessionCookieName = "sid";

    [HttpPost]
    public IActionResult Post()
    {
        if (Request.Cookies.TryGetValue(SessionCookieName, out var sid))
        {
            sessions.Remove(sid!);
            Response.Cookies.Delete(SessionCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
        }

        return Ok(new { message = "已登出" });
    }
}
