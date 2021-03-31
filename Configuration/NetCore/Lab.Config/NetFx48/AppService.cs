using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace NetFx48
{
    public interface IAppService
    {
        string GetPlayerId();
    }

    public class AppService : IAppService
    {
        private readonly IConfiguration _config;

        public AppService(IConfiguration config)
        {
            this._config = config;
        }

        public string GetPlayerId()
        {
            return this._config.GetSection("Player:AppId").Value;
        }
    }

    public class AppServiceWithOption : IAppService
    {
        private readonly AppSetting1 _appSetting;

        public AppServiceWithOption(IOptions<AppSetting1> options)
        {
            this._appSetting = options.Value;
        }
        public AppServiceWithOption(IOptionsSnapshot<AppSetting1> options)
        {
            this._appSetting = options.Value;
        }
        public AppServiceWithOption(IOptionsMonitor<AppSetting1> options)
        {
            this._appSetting = options.CurrentValue;
        } 
        public string GetPlayerId()
        {
            return this._appSetting.Player.AppId;
        }
    }
}