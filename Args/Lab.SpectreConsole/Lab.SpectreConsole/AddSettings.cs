using System.ComponentModel;
using Spectre.Console.Cli;

namespace Lab.SpectreConsole;
public class AddPackageCommand : Command<AddPackageSettings>
{
    public override int Execute(CommandContext context, AddPackageSettings settings)
    {
        // Omitted
        return 0;
    }
}

public class AddReferenceCommand : Command<AddReferenceSettings>
{
    public override int Execute(CommandContext context, AddReferenceSettings settings)
    {
        // Omitted
        return 0;
    }
}
public class AddSettings : CommandSettings
{
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }
}

public class AddPackageSettings : AddSettings
{
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public string PackageName { get; set; }

    [CommandOption("-v|--version <VERSION>")]
    public string Version { get; set; }
}

public class AddReferenceSettings : AddSettings
{
    [CommandArgument(0, "<PROJECT_REFERENCE>")]
    public string ProjectReference { get; set; }
}