using System;
using Microsoft.Extensions.Configuration;

namespace Lab.Infra
{
    public class AppSetting
    {
        private static readonly string MissSettingError = "Miss '{0}' section at config file";

        public ConnectionStrings ConnectionStrings { get; set; }

        public Player Player { get; set; }

        public static AppSetting Get(IConfiguration config)
        {
            var appSetting = new AppSetting
            {
                Player = new Player
                {
                    AppId = config["Player:AppId"],
                    Key   = config["Player:Key"],
                },
                ConnectionStrings = new ConnectionStrings
                {
                    DefaultConnectionString = config["ConnectionStrings:DefaultConnection"]
                }
            };

            return appSetting;
        }

        public static AppSetting GetIfNoSectionThrow(IConfiguration config)
        {
            var appSetting = new AppSetting
            {
                Player = new Player
                {
                    AppId = Get(config, "Player:AppId"),
                    Key   = Get(config, "Player:Key"),
                },
                ConnectionStrings = new ConnectionStrings
                {
                    DefaultConnectionString = Get(config, "ConnectionStrings:DefaultConnection")
                }
            };

            return appSetting;
        }

        private static string Get(IConfiguration config, string sectionName)
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