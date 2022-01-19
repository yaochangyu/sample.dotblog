using Microsoft.Extensions.Configuration;

namespace Lab.EnvFileConfig
{
    public class EnvFileConfigurationProvider : ConfigurationProvider
    {
        private readonly string _envFile;

        public EnvFileConfigurationProvider(string envFile)
        {
            this._envFile = envFile;
        }

        public override void Load()
        {
            if (!File.Exists(this._envFile))
            {
                return;
            }

            foreach (var line in File.ReadAllLines(this._envFile))
            {
                var parts = line.Split(
                                       '=',
                                       StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}