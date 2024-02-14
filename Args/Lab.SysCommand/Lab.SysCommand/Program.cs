// See https://aka.ms/new-console-template for more information

using System.CommandLine;

var rootCommand = new RootCommand();
var sub1Command = new Command("sub1", "First-level subcommand");
rootCommand.Add(sub1Command);
var sub1aCommand = new Command("sub1a", "Second level subcommand");
sub1Command.Add(sub1aCommand);

await rootCommand.InvokeAsync(args);
