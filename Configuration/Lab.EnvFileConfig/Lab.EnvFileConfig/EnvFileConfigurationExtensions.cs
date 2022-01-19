using Microsoft.Extensions.Configuration;

namespace Lab.EnvFileConfig
{
    public static class EnvFileConfigurationExtensions
    {
        public static IConfigurationBuilder AddEnvFile(this IConfigurationBuilder builder, string envFile)
        {
            var source = new EnvFileConfigurationSource(envFile);
            builder.Add(source);
            builder.AddEnvironmentVariables();
            return builder;
        }
    }
}