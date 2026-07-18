using System.CommandLine;
using ArchGen.Cli.Commands;
using ArchGen.Cli.Interactive;

if (args.Length == 0)
{
    var options = InteractiveMenu.Run();
    NewCommand.Execute(options);
    return 0;
}

var rootCommand = new RootCommand(
    "archgen — opinionated multi-architecture scaffolding CLI for .NET. " +
    "Generates N-Tier, Clean Architecture, CQRS, or Minimal API projects " +
    "with configurable persistence and UI type. " +
    "Run with no arguments for an interactive menu.")
{
    NewCommand.Build()
};

return await rootCommand.InvokeAsync(args);