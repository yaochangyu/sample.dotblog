using System.Threading.Tasks;
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

        //[AllowAnonymous]
        //[HttpPost("login")]
        //public ActionResult<string> login(LoginRequest login)
        //{
        //    return this._jwtProvider.Authenticate(login.UserId, login.Password);
        //}

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginRequest login)
        {
            var token = this._jwtProvider.Authenticate(login.UserId, login.Password);
            if (string.IsNullOrWhiteSpace(token))
            {
                IActionResult result = new BadRequestObjectResult(new {Message = "Invalid Authorization Header"});
                return result;
            }

            return this.Ok(token);
        }

        private bool ValidateUser(LoginRequest login)
        {
            return true; // TODO
        }
    }

    public class LoginRequest
    {
        public string UserId { get; set; }

        public string Password { get; set; }
    }
}