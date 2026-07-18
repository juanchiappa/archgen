using ArchGen.Cli.Generators;
using ArchGen.Cli.Options;
using ArchGen.Cli.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public sealed class NTierPattern : IArchitecturePattern
    {
        public string Id => "ntier";
        public string DisplayName => "N-Tier";

        public void Generate(ProjectOptions options)
        {
            var solutionDirectory = SolutionGenerator.CreateSolution(options);

            var entitiesProjectName = $"{options.ProjectName}.Entities";
            var dataAccessProjectName = $"{options.ProjectName}.DataAccess";
            var businessLogicProjectName = $"{options.ProjectName}.BusinessLogic";

            var entitiesDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, entitiesProjectName);
            var dataAccessDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, dataAccessProjectName);
            var businessLogicDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, businessLogicProjectName);

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"),
                Path.Combine(entitiesDir, $"{entitiesProjectName}.csproj"));

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(businessLogicDir, $"{businessLogicProjectName}.csproj"),
                Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"));

            GenerateUiProject(solutionDirectory, businessLogicDir, businessLogicProjectName, options);

            var rootNamespace = $"{options.ProjectName}.DataAccess";
            var persistenceGenerator = PersistenceRegistry.Resolve(options.Persistence);
            persistenceGenerator.Generate(dataAccessDir, rootNamespace, options);

            foreach (var (packageId, version) in persistenceGenerator.RequiredPackages(options))
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory,
                    Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"),
                    packageId,
                    version);
            }

            WriteReadme(solutionDirectory, options);
        }

        private static void GenerateUiProject(
    string solutionDirectory,
    string businessLogicDir,
    string businessLogicProjectName,
    ProjectOptions options)
        {
            var uiProjectName = $"{options.ProjectName}.UI";

            string uiDir = options.Ui switch
            {
                UiKind.Console => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiProjectName),
                UiKind.Api => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiProjectName),
                _ => throw new NotSupportedException(
                    $"UI type '{options.Ui}' is not implemented yet (planned for a later phase). " +
                    "Use --ui console or --ui api for now.")
            };

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiProjectName}.csproj"),
                Path.Combine(businessLogicDir, $"{businessLogicProjectName}.csproj"));
        }

        private static void WriteReadme(string solutionDirectory, ProjectOptions options)
        {
            var content = $"""
            # {options.ProjectName}

            Generated with archgen using the **N-Tier** architecture pattern.

            ## Layers

            - `{options.ProjectName}.Entities` — plain domain entities, no dependencies.
            - `{options.ProjectName}.DataAccess` — persistence layer ({options.Persistence}).
            - `{options.ProjectName}.BusinessLogic` — application/business rules, depends on DataAccess.
            - `{options.ProjectName}.UI` — {options.Ui} entry point, depends on BusinessLogic.

            ## Getting started

            ```bash
            dotnet restore
            dotnet build
            dotnet run --project src/{options.ProjectName}.UI
            ```
            """;

            File.WriteAllText(Path.Combine(solutionDirectory, "README.md"), content);
        }

    }
}
