using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiCore31;

namespace JwtAuthDemo.Controllers
{
    /// <summary>
    ///     主要負責 JWT Token 相關操作
    /// </summary>
    [Authorize]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IJwtAuthenticationProvider _jwtProvider;

        public TokenController(IJwtAuthenticationProvider jwtProvider)
        {
            this._jwtProvider = jwtProvider;
        }

        /// <summary>
        ///     登入並取得 JWT Token
        /// </summary>
        /// <param name="login">LoginViewModel</param>
        /// <returns>回傳一個 JWT Token 字串</returns>
        /// <example>
        ///     // 這個沒效！回傳純字串是沒效的，要自訂 ViewModel 才能設定範例到特定屬性去
        ///     "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsImtpZCI6bnVsbCwidHlwIjoiSldUIn0.eyJleHAiOiIxNTc2NTA5NzA0IiwiaXNzIjoiSnd0QXV0aERlbW8iLCJpYXQiOiIxNTc2NTA3OTA0IiwibmJmIjoiMTU3NjUwNzkwNCJ9.XmagvhyW_6SUFJfiOahkOBuVlLjyogEzMba3-WlbNmI"
        /// </example>
        /// <response code="200">成功產生 JWT Token</response>
        /// <response code="400">登入帳號或密碼錯誤</response>
        [AllowAnonymous]
        [HttpPost("~/signin")]
        public ActionResult<string> SignIn(LoginViewModel login)
        {
            return this._jwtProvider.Authenticate(login.Username, login.Password);
        }

        private bool ValidateUser(LoginViewModel login)
        {
            return true; // TODO
        }
    }

    /// <summary>
    ///     登入模型
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        ///     使用者名稱
        /// </summary>
        /// <example>"will"</example>
        public string Username { get; set; }

        /// <summary>
        ///     使用者密碼
        /// </summary>
        /// <example>"YourPassword"</example>
        public string Password { get; set; }
    }
}