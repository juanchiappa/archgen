using ArchGen.Cli.Generators;
using ArchGen.Cli.Options;
using ArchGen.Cli.Patterns.Cqrs;
using ArchGen.Cli.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public sealed class CqrsPattern : IArchitecturePattern
    {
        public string Id => "cqrs";
        public string DisplayName => "CQRS";

        public void Generate(ProjectOptions options)
        {
            var solutionDirectory = SolutionGenerator.CreateSolution(options);

            var domainName = $"{options.ProjectName}.Domain";
            var applicationName = $"{options.ProjectName}.Application";
            var infrastructureName = $"{options.ProjectName}.Infrastructure";
            var uiName = $"{options.ProjectName}.UI";

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

            string uiDir = options.Ui switch
            {
                UiKind.Console => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiName),
                UiKind.Api => SolutionGenerator.CreateWebApiProject(solutionDirectory, uiName),
                _ => throw new NotSupportedException(
                    $"UI type '{options.Ui}' is not implemented yet for the CQRS pattern. " +
                    "Use --ui console or --ui api for now.")
            };

            SolutionGenerator.AddProjectReference(
                solutionDirectory, Path.Combine(uiDir, $"{uiName}.csproj"), Path.Combine(applicationDir, $"{applicationName}.csproj"));
            SolutionGenerator.AddProjectReference(
                solutionDirectory, Path.Combine(uiDir, $"{uiName}.csproj"), Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"));

            File.WriteAllText(
                Path.Combine(domainDir, "ExampleItem.cs"), ExampleFeatureTemplates.BuildExampleEntity(domainName));

            var persistenceGenerator = PersistenceRegistry.Resolve(options);
            persistenceGenerator.GenerateAbstraction(domainDir, domainName);

            File.WriteAllText(
                Path.Combine(domainDir, "Mediator.Contracts.cs"), MediatorTemplates.BuildMediatorContracts(domainName));

            persistenceGenerator.GenerateImplementation(
                infrastructureDir, infrastructureName, domainName,
                entitiesAssemblyName: domainName, entitiesNamespace: domainName, options);

            foreach (var (packageId, version) in persistenceGenerator.RequiredPackages(options))
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory, Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"), packageId, version);
            }

            File.WriteAllText(
                Path.Combine(infrastructureDir, "Mediator.cs"),
                MediatorTemplates.BuildMediatorImplementation(domainName, infrastructureName));

            var concreteClassName = PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);
            File.WriteAllText(Path.Combine(infrastructureDir, "DependencyInjection.cs"), $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{domainName}};

            namespace {{infrastructureName}};

            public static class DependencyInjection
            {
                public static IServiceCollection AddInfrastructure(this IServiceCollection services)
                {
                    services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();
                    services.AddScoped<IMediator, Mediator>();
                    return services;
                }
            }

            """);

            SolutionGenerator.AddPackage(
                solutionDirectory, Path.Combine(infrastructureDir, $"{infrastructureName}.csproj"),
                "Microsoft.Extensions.DependencyInjection.Abstractions", "8.0.0");

            var commandsDir = Path.Combine(applicationDir, "Commands");
            var queriesDir = Path.Combine(applicationDir, "Queries");
            Directory.CreateDirectory(commandsDir);
            Directory.CreateDirectory(queriesDir);

            File.WriteAllText(
                Path.Combine(commandsDir, "CreateExampleItemCommand.cs"),
                ExampleFeatureTemplates.BuildCreateCommand(domainName, applicationName));

            File.WriteAllText(
                Path.Combine(queriesDir, "GetAllExampleItemsQuery.cs"),
                ExampleFeatureTemplates.BuildGetAllQuery(domainName, applicationName));

            File.WriteAllText(Path.Combine(applicationDir, "DependencyInjection.cs"), $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{domainName}};
            using {{applicationName}}.Commands;
            using {{applicationName}}.Queries;

            namespace {{applicationName}};

            public static class DependencyInjection
            {
                public static IServiceCollection AddApplication(this IServiceCollection services)
                {
                    services.AddScoped<ICommandHandler<CreateExampleItemCommand, int>, CreateExampleItemCommandHandler>();
                    services.AddScoped<IQueryHandler<GetAllExampleItemsQuery, List<ExampleItem>>, GetAllExampleItemsQueryHandler>();
                    return services;
                }
            }

            """);

            SolutionGenerator.AddPackage(
                solutionDirectory, Path.Combine(applicationDir, $"{applicationName}.csproj"),
                "Microsoft.Extensions.DependencyInjection.Abstractions", "8.0.0");

            // --- UI: composition root ---
            GenerateUiHost(uiDir, uiName, domainName, applicationName, infrastructureName, options);

            WriteReadme(solutionDirectory, options);
        }

        private static void GenerateUiHost(
            string uiDir, string uiName, string domainName, string applicationName, string infrastructureName,
            ProjectOptions options)
        {
            var content = options.Ui switch
            {
                UiKind.Api => $$"""
                using Microsoft.Extensions.DependencyInjection;
                using {{domainName}};
                using {{applicationName}};
                using {{applicationName}}.Commands;
                using {{applicationName}}.Queries;
                using {{infrastructureName}};

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddApplication();
                builder.Services.AddInfrastructure();

                var app = builder.Build();

                app.MapPost("/items", async (IMediator mediator, string name) =>
                    await mediator.Send(new CreateExampleItemCommand { Name = name }));

                app.MapGet("/items", async (IMediator mediator) =>
                    await mediator.Send(new GetAllExampleItemsQuery()));

                app.Run();
                """,
                _ => $$"""
                using Microsoft.Extensions.DependencyInjection;
                using {{domainName}};
                using {{applicationName}};
                using {{applicationName}}.Commands;
                using {{applicationName}}.Queries;
                using {{infrastructureName}};

                var services = new ServiceCollection();
                services.AddApplication();
                services.AddInfrastructure();

                using var serviceProvider = services.BuildServiceProvider();
                var mediator = serviceProvider.GetRequiredService<IMediator>();

                var newId = await mediator.Send(new CreateExampleItemCommand { Name = "Sample" });
                var items = await mediator.Send(new GetAllExampleItemsQuery());

                Console.WriteLine("{{options.ProjectName}} is running.");
                Console.WriteLine($"Created ExampleItem with Id {newId} via mediator (Command).");
                Console.WriteLine($"Total items via mediator query: {items.Count} (Query).");
                """
            };

            File.WriteAllText(Path.Combine(uiDir, "Program.cs"), content);

            if (options.Ui != UiKind.Api)
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory: Path.GetDirectoryName(Path.GetDirectoryName(uiDir))!,
                    Path.Combine(uiDir, $"{uiName}.csproj"),
                    "Microsoft.Extensions.DependencyInjection",
                    "8.0.0");
            }
        }

        private static void WriteReadme(string solutionDirectory, ProjectOptions options)
        {
            var content = $"""
            # {options.ProjectName}

            Generated with archgen using the **CQRS** architecture pattern.

            ## Layers

            - `{options.ProjectName}.Domain` — entities, `IPersistenceProvider`, and mediator contracts (`ICommand`, `IQuery`, `IMediator`). No dependencies.
            - `{options.ProjectName}.Application` — `Commands/` and `Queries/`, each with its own handler. Depends on Domain only.
            - `{options.ProjectName}.Infrastructure` — persistence ({options.Persistence}) and the mediator implementation. Depends on Domain only.
            - `{options.ProjectName}.UI` — {options.Ui} entry point (composition root). Wires Application + Infrastructure together via `AddApplication()` and `AddInfrastructure()`.

            ## Example vertical slice

            A working example (`ExampleItem` + `CreateExampleItemCommand` + `GetAllExampleItemsQuery`) is included end-to-end, dispatched through `IMediator` — replace it with your own commands/queries as you build out the domain.

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
