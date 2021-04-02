using Microsoft.Extensions.Configuration;

namespace NetFx48
{
    public class AppWorkFlow : IAppWorkFlow
    {
        private readonly IConfiguration _config;

        public AppWorkFlow(IConfiguration config)
        {
            this._config = config;
        }

        public string GetPlayerId()
        {
            return this._config.GetSection("Player:AppId").Value;
        }
    }
}