using System;
using Microsoft.Extensions.Options;

namespace NetFx48
{
    public class AppServiceWithOptionsMonitor : IAppService
    {
        private readonly AppSetting1 _appSetting;
        private readonly Player1     _player;

        public AppServiceWithOptionsMonitor(IOptionsMonitor<AppSetting1> appSettingOption,
                                            IOptionsMonitor<Player1>     playerOption)
        {
            this._player     = playerOption.Get("Player");
            this._appSetting = appSettingOption?.CurrentValue;

            Console.WriteLine($"AppSetting.Player.AppId = {this._appSetting.Player.AppId}");
            Console.WriteLine($"Player.AppId = {this._player.AppId}");
        }

        public string GetPlayerId()
        {
            return this._player.AppId;
        }
    }
}