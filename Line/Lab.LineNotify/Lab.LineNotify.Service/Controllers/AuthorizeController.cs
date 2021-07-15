using System.Threading;
using System.Threading.Tasks;
using Lab.LineBot.SDK;
using Lab.LineBot.SDK.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lab.LineNotify.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorizeCodeController : ControllerBase
    {
        private readonly IConfiguration                   _config;
        private readonly ILineNotifyProvider              _lineNotifyProvider;
        private readonly ILogger<AuthorizeCodeController> _logger;

        public AuthorizeCodeController(ILogger<AuthorizeCodeController> logger,
                                       IConfiguration                   config,
                                       ILineNotifyProvider              lineNotifyProvider)
        {
            this._logger             = logger;
            this._config             = config;
            this._lineNotifyProvider = lineNotifyProvider;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] IFormCollection data, CancellationToken cancelToken)
        {
            if (data.TryGetValue("code", out var code) == false)
            {
                this.ModelState.AddModelError("code 欄位", "必填");
                return this.BadRequest(this.ModelState);
            }

            if (data.TryGetValue("state", out var state) == false)
            {
                this.ModelState.AddModelError("state 欄位", "必填");
                return this.BadRequest(this.ModelState);
            }

            var config             = this._config;
            var lineNotifyProvider = this._lineNotifyProvider;

            var lineConfig = config.GetSection("LineNotify");
            var request = new TokenRequest
            {
                Code         = code,
                ClientId     = lineConfig.GetValue<string>("clientId"),
                ClientSecret = lineConfig.GetValue<string>("clientSecret"),
                CallbackUrl  = lineConfig.GetValue<string>("redirectUri"),
            };
            var accessToken = await lineNotifyProvider.GetAccessTokenAsync(request, cancelToken);

            //TODO:應該記錄在你的 DB 或是其它地方，不應該回傳 Access Token
            return this.Ok(accessToken);
        }
    }
}