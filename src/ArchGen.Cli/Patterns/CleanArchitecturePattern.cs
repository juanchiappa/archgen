using ArchGen.Cli.Generators;
using ArchGen.Cli.Options;
using ArchGen.Cli.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public sealed class CleanArchitecturePattern : IArchitecturePattern
    {
        public string Id => "clean-architecture";
        public string DisplayName => "Clean Architecture";

        public void Generate(ProjectOptions options)
        {
            var solutionDirectory = SolutionGenerator.CreateSolution(options);

            var domainName = $"{options.ProjectName}.Domain";
            var applicationName = $"{options.ProjectName}.Application";
            var infrastructureName = $"{options.ProjectName}.Infrastructure";

            var domainDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, domainName);
            var applicationDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, applicationName);
            var infrastructureDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, infrastructureName);

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(applicationDir, $"{applicationName}.csproj"),
                Path.Combine(domainDir, $"{domainName}.csproj"));

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"),
                Path.Combine(domainDir, $"{domainName}.csproj"));

            var uiDir = GenerateUiProject(
                solutionDirectory, applicationDir, applicationName, infrastructureDir, infrastructureName, options);

            var domainNamespace = domainName;
            var infrastructureNamespace = infrastructureName;

            var persistenceGenerator = PersistenceRegistry.Resolve(options.Persistence);

            persistenceGenerator.GenerateAbstraction(domainDir, domainNamespace);

            persistenceGenerator.GenerateImplementation(
                infrastructureDir, infrastructureNamespace, domainNamespace,
                entitiesAssemblyName: domainName, entitiesNamespace: domainNamespace, options);
            foreach (var (packageId, version) in persistenceGenerator.RequiredPackages(options))
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory,
                    Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"),
                    packageId,
                    version);
            }

            WriteReadme(solutionDirectory, options);
        }

        private static string GenerateUiProject(
            string solutionDirectory,
            string applicationDir,
            string applicationName,
            string infrastructureDir,
            string infrastructureName,
            ProjectOptions options)
        {
            var uiName = $"{options.ProjectName}.UI";

            string uiDir = options.Ui switch
            {
                UiKind.Console => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiName),
                UiKind.Api => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiName),
                _ => throw new NotSupportedException(
                    $"UI type '{options.Ui}' is not implemented yet (planned for a later phase). " +
                    "Use --ui console or --ui api for now.")
            };

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiName}.csproj"),
                Path.Combine(applicationDir, $"{applicationName}.csproj"));

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiName}.csproj"),
                Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"));

            return uiDir;
        }

        private static void WriteReadme(string solutionDirectory, ProjectOptions options)
        {
            var content = $"""
            # {options.ProjectName}

            Generated with archgen using the **Clean Architecture** pattern.

            ## Layers

            - `{options.ProjectName}.Domain` — entities and abstractions (e.g. `IPersistenceProvider`). No dependencies.
            - `{options.ProjectName}.Application` — use cases / business rules. Depends on Domain only.
            - `{options.ProjectName}.Infrastructure` — concrete implementations ({options.Persistence}). Depends on Domain, implements its interfaces.
            - `{options.ProjectName}.UI` — {options.Ui} entry point. Depends on Application and Infrastructure to wire everything together.

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
