using Spectre.Console.Cli;

namespace Lab.EnvGeneratorCli;

internal sealed class ConvertEnvCommand : Command<ConvertEnvCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--env")]
        public string? Environment { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // 讀取 env.template 檔案
        var envTemplate = File.ReadAllLines("env.template");

        var env = settings.Environment;

        var outputFileName = $"app.{env}.env";

        // 解析 env.template 檔案
        var contents = ParseEnvTemplate(envTemplate, env);
        GenerateEnvFile(contents, outputFileName);
        Console.WriteLine($"Generated {outputFileName}.");

        return 0;
    }

    private static void GenerateEnvFile(Dictionary<string, string> settings, string outputFileName)
    {
        using var writer = new StreamWriter(outputFileName);
        foreach (var setting in settings)
        {
            writer.WriteLine($"{setting.Key}={setting.Value}");
        }
    }

    private static Dictionary<string, string> ParseEnvTemplate(string[] templateLines, string env)
    {
        var result = new Dictionary<string, string>();
        var currentSection = "";
        foreach (var line in templateLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            // 找出 section
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Trim('[', ']');
                continue;
            }

            var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if (parts.Length > 2)
            {
                // 被分割的部分重新組合
                value = string.Join("=", parts.Skip(1)).Trim();
            }

            // 優先取環境變數的值
            if (key == env)
            {
                result[currentSection] = value;
            }

            // 若沒有環境變數的值，則取 default
            else if (key == "default")
            {
                result.TryAdd(currentSection, value);
            }
        }

        return result;
    }
}