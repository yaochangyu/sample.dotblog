using Microsoft.Extensions.Options;

namespace NetFx48
{
    public class AppWorkFlowWithOption : IAppWorkFlow
    {
        private readonly AppSetting1 _appSetting;

        public AppWorkFlowWithOption(IOptions<AppSetting1> options)
        {
            this._appSetting = options.Value;
        }
    
        public string GetPlayerId()
        {
            return this._appSetting.Player.AppId;
        }
    }
}