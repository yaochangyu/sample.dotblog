using Microsoft.Extensions.Configuration;

namespace Lab.EnvFileConfig
{
    public class EnvFileConfigurationSource : IConfigurationSource
    {
        private readonly string _envFile;

        public EnvFileConfigurationSource(string envFile)
        {
            this._envFile = envFile;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvFileConfigurationProvider(this._envFile);
        }
    }
}