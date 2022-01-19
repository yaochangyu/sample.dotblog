using System;
using Microsoft.Extensions.Options;

namespace NetFx48
{
    public class AppWorkFlowWithOption : IAppWorkFlow
    {
        private readonly AppSetting1 _appSetting;

        public AppWorkFlowWithOption(IOptions<AppSetting1> options)
        {
            try
            {
                this._appSetting = options.Value;
            }
            catch (OptionsValidationException ex)
            {
                foreach (var failure in ex.Failures)
                {
                    Console.WriteLine(failure);
                }
            }
        }

        public string GetPlayerId()
        {
            return this._appSetting.Player.AppId;
        }
    }
}