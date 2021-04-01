using System;
using Microsoft.Extensions.Options;

namespace NetFx48
{
    public class AppServiceWithOptionsSnapshot : IAppService
    {
        private readonly AppSetting1 _appSetting;
        private readonly Player1     _player;

        public AppServiceWithOptionsSnapshot(IOptionsSnapshot<AppSetting1> appSettingOption,
                                             IOptionsSnapshot<Player1>     playerOption)
        {
            this._player     = playerOption?.Value;
            this._appSetting = appSettingOption?.Value;

            Console.WriteLine($"AppSetting.Player.AppId = {this._appSetting.Player.AppId}");
            Console.WriteLine($"Player.AppId = {this._player.AppId}");
        }

        public string GetPlayerId()
        {
            return this._player.AppId;
        }
    }
}