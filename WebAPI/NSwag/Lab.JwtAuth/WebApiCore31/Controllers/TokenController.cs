using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiCore31;

namespace JwtAuthDemo.Controllers
{
    [Authorize]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IJwtAuthenticationProvider _jwtProvider;

        public TokenController(IJwtAuthenticationProvider jwtProvider)
        {
            this._jwtProvider = jwtProvider;
        }

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
    }
}