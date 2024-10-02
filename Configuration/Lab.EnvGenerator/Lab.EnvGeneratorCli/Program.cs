using Spectre.Console.Cli;

namespace Lab.EnvGeneratorCli;

internal class Program
{
    private static void Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<ConvertEnvCommand>("convert")
                .WithDescription("convert a file.")
                .WithExample("convert", "--env", "qa");
        });

        app.Run(args);
    }
}