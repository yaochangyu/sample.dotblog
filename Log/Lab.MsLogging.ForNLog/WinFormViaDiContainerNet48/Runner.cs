using Microsoft.Extensions.Logging;

namespace WinFormViaDiContainerNet48
{
    public class Runner
    {
        private readonly ILogger<Runner> _logger;

        public Runner(ILogger<Runner> logger)
        {
            this._logger = logger;
        }

        public void DoAction(string name)
        {
            this._logger.LogInformation(20, "Doing hard work! {Action}", name);
        }
    }
}