using Microsoft.Extensions.Configuration;

namespace NetFx48
{
    public class AppService
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
}