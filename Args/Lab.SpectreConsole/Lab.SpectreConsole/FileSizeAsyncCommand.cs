using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lab.SpectreConsole;

internal sealed class FileSizeAsyncCommand : CancellableAsyncCommand<FileSizeAsyncCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[searchPath]")]
        public string? SearchPath { get; init; }

        [CommandOption("-p|--pattern")]
        public string? SearchPattern { get; init; }

        [CommandOption("--hidden")]
        [DefaultValue(true)]
        public bool IncludeHidden { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        Settings settings,
        CancellationToken cancellation)
    {
        await Task.Delay(5000, cancellation);

        var searchOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System
        };

        var searchPattern = settings.SearchPattern ?? "*.*";
        var searchPath = settings.SearchPath ?? Directory.GetCurrentDirectory();
        var files = new DirectoryInfo(searchPath)
            .GetFiles(searchPattern, searchOptions);

        var totalFileSize = files
            .Sum(fileInfo => fileInfo.Length);

        AnsiConsole.MarkupLine(
            $"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");

        return 0;
    }

    public FileSizeAsyncCommand(ILogger<CancellableAsyncCommand<Settings>> logger)
        : base(logger)
    {
    }
}