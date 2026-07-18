using System;
using System.Collections.Generic;
using System.Text;
using ArchGen.Cli.Options;
using Spectre.Console;

namespace ArchGen.Cli.Interactive
{
    public static class InteractiveMenu
    {
        public static ProjectOptions Run()
        {
            AnsiConsole.Write(new FigletText("archgen").Color(Color.MediumPurple1));
            var projectName = AnsiConsole.Ask<string>("[bold]Project name[/]:");

            var pattern = AnsiConsole.Prompt(
                new SelectionPrompt<ArchitecturePattern>()
                    .Title("Choose an [bold]architecture pattern[/]:")
                    .AddChoices(
                    ArchitecturePattern.NTier,
                    ArchitecturePattern.CleanArchitecture,
                    ArchitecturePattern.Cqrs,
                    ArchitecturePattern.MinimalApi
                    ));

            var persistence = AnsiConsole.Prompt(
                new SelectionPrompt<PersistenceKind>()
                    .Title("Choose a [bold]persistence[/] backend:")
                    .AddChoices(
                    PersistenceKind.Json,
                    PersistenceKind.Sqlite,
                    PersistenceKind.Postgres));

            var orm = persistence == PersistenceKind.Json ? OrmKind.EfCore : AnsiConsole.Prompt(
                new SelectionPrompt<OrmKind>()
                    .Title("Choose an [bold]ORM[/]:")
                    .AddChoices(OrmKind.EfCore, OrmKind.Dapper));

            var ui = AnsiConsole.Prompt(
                new SelectionPrompt<UiKind>()
                .Title("Choose a [bold]UI[/] type:")
                .AddChoices(
                    UiKind.Console,
                    UiKind.Api,
                    UiKind.WinForms,
                    UiKind.Wpf,
                    UiKind.Blazor));

            var outputDirectory = AnsiConsole.Ask(
                "[bold]Output directory[/]:", Directory.GetCurrentDirectory());

            return new ProjectOptions
            {
                ProjectName = projectName,
                OutputDirectory = outputDirectory,
                Pattern = pattern,
                Persistence = persistence,
                Orm = orm,
                Ui = ui
            };
        }
    }
}
