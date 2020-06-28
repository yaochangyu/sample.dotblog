using System;
using Microsoft.Extensions.Configuration;

namespace Lab.Infra
{
    public class AppSetting
    {
        private static readonly string MissSettingError = "Miss {0} at coifig's json file";

        public ConnectionStrings ConnectionStrings { get; set; }

        public Player Player { get; set; }

        public AppSetting()
        {
            
        }
        private readonly IConfiguration _configruration;

        public AppSetting(IConfiguration configuration)
        {
            this._configruration = configuration;
            //    this.Player = new Player
            //    {
            //        AppId = configuration["Player:AppId"],
            //        Key   = configuration["Player:Key"],
            //    };

            //    this.DefaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public AppSetting GetIfNoSectionThrow()
        {
            var config = this._configruration;
            var appSetting = new AppSetting(config)
            {
                Player = new Player
                {
                    AppId = this.Get(config, "Player:AppId"),
                    Key   = this.Get(config, "Player:Key"),
                },
                //DefaultConnectionString = this.Get(config, "ConnectionStrings:DefaultConnection")
            };

            return appSetting;
        }

        private string Get(IConfiguration config, string sectionName)
        {
            var setting    = config[sectionName];
            var hasSetting = string.IsNullOrWhiteSpace(setting);
            if (hasSetting == false)
            {
                throw new Exception(string.Format(MissSettingError, sectionName));
            }

            return config[sectionName];
        }
    }
}