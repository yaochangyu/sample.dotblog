namespace NetFx48
{
    public class AppWorkFlow1 : IAppWorkFlow
    {
        private AppSetting _appSetting;

        public AppWorkFlow1(AppSetting appSetting)
        {
            this._appSetting = appSetting;
        }
        public string GetPlayerId()
        {
            return this._appSetting.Player.AppId;
        }
    }
}