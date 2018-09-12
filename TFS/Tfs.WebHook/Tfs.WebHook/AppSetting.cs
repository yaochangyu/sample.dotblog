using System.Configuration;

namespace Tfs.WebHook
{
    public class AppSetting
    {
        private static readonly string MissSettingError = "Miss {0} in Web.config or App.config";

        public class Teams
        {
            private static string _incomingUrl;

            public static string IncomingUrl
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(_incomingUrl))
                    {
                        var key = "Teams:IncomingUrl";
                        var appSetting = ConfigurationManager.AppSettings[key];
                        if (appSetting == null)
                        {
                            throw new ConfigurationErrorsException(string.Format(MissSettingError, key));
                        }

                        _incomingUrl = appSetting;
                    }

                    return _incomingUrl;
                }
                set => _incomingUrl = value;
            }
        }
    }
}