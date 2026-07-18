using ArchGen.Cli.Options;
using ArchGen.Cli.Patterns;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace ArchGen.Cli.Commands
{
    public static class NewCommand
    {
        public static Command Build()
        {
            var nameArgument = new Argument<string>(
    name: "name",
    description: "Name of the project to generate.");

            var patternOption = new Option<ArchitecturePattern>(
                aliases: new[] { "--pattern", "-p" },
                getDefaultValue: () => ArchitecturePattern.NTier,
                description: "Architecture pattern to scaffold.");

            var persistenceOption = new Option<PersistenceKind>(
                aliases: new[] { "--persistence" },
                getDefaultValue: () => PersistenceKind.Json,
                description: "Persistence backend.");

            var ormOption = new Option<OrmKind>(
                aliases: new[] { "--orm" },
                getDefaultValue: () => OrmKind.EfCore,
                description: "ORM/data-access technology (ignored for JSON persistence).");

            var uiOption = new Option<UiKind>(
                aliases: new[] { "--ui" },
                getDefaultValue: () => UiKind.Console,
                description: "UI project type to scaffold on top of the architecture.");

            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                getDefaultValue: () => Directory.GetCurrentDirectory(),
                description: "Directory where the project folder will be created.");

            var gitOption = new Option<bool>(
                aliases: new[] { "--git" },
                getDefaultValue: () => false,
                description: "Initialize git and publish to GitHub (requires 'gh' installed and authenticated).");

            var command = new Command("new", "Scaffold a new .NET project.")
        {
            nameArgument,
            patternOption,
            persistenceOption,
            ormOption,
            uiOption,
            outputOption,
            gitOption
        };

            command.SetHandler(context =>
            {
                var options = new ProjectOptions
                {
                    ProjectName = context.ParseResult.GetValueForArgument(nameArgument),
                    OutputDirectory = context.ParseResult.GetValueForOption(outputOption)!,
                    Pattern = context.ParseResult.GetValueForOption(patternOption),
                    Persistence = context.ParseResult.GetValueForOption(persistenceOption),
                    Orm = context.ParseResult.GetValueForOption(ormOption),
                    Ui = context.ParseResult.GetValueForOption(uiOption)
                };

                var enableGit = context.ParseResult.GetValueForOption(gitOption);

                Execute(options, enableGit);
            });
            return command;
        }

        public static void Execute(ProjectOptions options, bool enableGit = false)
        {
            Console.WriteLine($"Scaffolding '{options.ProjectName}' " +
                $"[pattern={options.Pattern}, persistence={options.Persistence}, ui={options.Ui}]...");

            try
            {
                var generator = PatternRegistry.Resolve(options.Pattern);
                generator.Generate(options);

                var solutionDirectory = Path.Combine(options.OutputDirectory, options.ProjectName);
                Console.WriteLine($"Done. Project created at: {solutionDirectory}");

                if (enableGit)
                {
                    Generators.GitIntegration.TryInitializeAndPublish(solutionDirectory, options.ProjectName);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }
    }
}
