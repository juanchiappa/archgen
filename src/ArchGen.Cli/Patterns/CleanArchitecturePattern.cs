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

            var persistenceGenerator = PersistenceRegistry.Resolve(options);
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

            GenerateDependencyInjectionWiring(
                solutionDirectory,
                infrastructureDir, infrastructureName, infrastructureNamespace,
                domainNamespace,
                uiDir, uiName: $"{options.ProjectName}.UI",
                options);

            WriteReadme(solutionDirectory, options);
        }

        private static void GenerateDependencyInjectionWiring(
            string solutionDirectory,
            string infrastructureDir,
            string infrastructureName,
            string infrastructureNamespace,
            string domainNamespace,
            string uiDir,
            string uiName,
            ProjectOptions options)
        {
            var concreteClassName = PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);

            var diExtensionContent = $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{domainNamespace}};

            namespace {{infrastructureNamespace}};


            public static class DependencyInjection
            {
                public static IServiceCollection AddInfrastructure(this IServiceCollection services)
                {
                    services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();
                    return services;
                }
            }

            """;

            File.WriteAllText(Path.Combine(infrastructureDir, "DependencyInjection.cs"), diExtensionContent);

            SolutionGenerator.AddPackage(
                solutionDirectory,
                Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"),
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "8.0.0");

            var programContent = options.Ui == UiKind.Api
                            ? $$"""
                    using Microsoft.Extensions.DependencyInjection;
                    using {{infrastructureNamespace}};
                    using {{domainNamespace}};

                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddInfrastructure();

                    var app = builder.Build();

                    app.MapGet("/", (IPersistenceProvider persistenceProvider) =>
                        $"{{options.ProjectName}} API is running. Persistence provider resolved via DI: {persistenceProvider.GetType().Name}");

                    app.Run();
                    """
                            : $$"""
                    using Microsoft.Extensions.DependencyInjection;
                    using {{infrastructureNamespace}};
                    using {{domainNamespace}};

                    var services = new ServiceCollection();
                    services.AddInfrastructure();

                    using var serviceProvider = services.BuildServiceProvider();
                    var persistenceProvider = serviceProvider.GetRequiredService<IPersistenceProvider>();

                    Console.WriteLine("{{options.ProjectName}} is running.");
                    Console.WriteLine($"Persistence provider resolved via DI: {persistenceProvider.GetType().Name}");
                    """;

            File.WriteAllText(Path.Combine(uiDir, "Program.cs"), programContent);
            if (options.Ui != UiKind.Api)
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory,
                    Path.Combine(uiDir, $"{uiName}.csproj"),
                    "Microsoft.Extensions.DependencyInjection",
                    "8.0.0");
            }
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
                UiKind.Api => SolutionGenerator.CreateWebApiProject(solutionDirectory, uiName),
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
