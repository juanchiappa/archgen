using ArchGen.Cli.Generators;
using ArchGen.Cli.Options;
using ArchGen.Cli.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public sealed class MinimalApiPattern : IArchitecturePattern
    {
        public string Id => "minimal-api";
        public string DisplayName => "Minimal API";

        public void Generate(ProjectOptions options)
        {
            var solutionDirectory = SolutionGenerator.CreateSolution(options);

            var projectName = options.ProjectName;
            var projectNamespace = projectName;

            var projectDir = SolutionGenerator.CreateWebApiProject(solutionDirectory, projectName);

            File.WriteAllText(Path.Combine(projectDir, "ExampleItem.cs"), $$"""
            namespace {{projectNamespace}};

            public class ExampleItem
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            """);

            var persistenceGenerator = PersistenceRegistry.Resolve(options);
            persistenceGenerator.GenerateAbstraction(projectDir, projectNamespace);
            persistenceGenerator.GenerateImplementation(
                projectDir, projectNamespace, projectNamespace,
                entitiesAssemblyName: projectName, entitiesNamespace: projectNamespace, options);

            foreach (var (packageId, version) in persistenceGenerator.RequiredPackages(options))
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory, Path.Combine(projectDir, $"{projectName}.csproj"), packageId, version);
            }

            var concreteClassName = PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);

            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), $$"""
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();

            var app = builder.Build();

            app.MapGet("/items", (IPersistenceProvider persistence) => persistence.GetAll<ExampleItem>());

            app.MapGet("/items/{id}", (int id, IPersistenceProvider persistence) =>
                persistence.GetById<ExampleItem>(id) is { } item ? Results.Ok(item) : Results.NotFound());

            app.MapPost("/items", (string name, IPersistenceProvider persistence) =>
            {
                var item = new ExampleItem { Name = name };
                persistence.Save(item);
                return Results.Created($"/items/{item.Id}", item);
            });

            app.MapDelete("/items/{id}", (int id, IPersistenceProvider persistence) =>
            {
                persistence.Delete<ExampleItem>(id);
                return Results.NoContent();
            });

            app.Run();
            """);

            WriteReadme(solutionDirectory, options);
        }

        private static void WriteReadme(string solutionDirectory, ProjectOptions options)
        {
            var content = $$"""
            # {{options.ProjectName}}

            Generated with archgen using the **Minimal API** architecture pattern.

            Unlike N-Tier, Clean Architecture, or CQRS, this pattern has no
            separate layers — entities, persistence ({{options.Persistence}}), and
            HTTP endpoints all live in a single project (`src/{{options.ProjectName}}`).
            This trades structure for speed: it's the fastest path to a working
            API, at the cost of the separation of concerns the other patterns
            provide. A good fit for small services or prototypes; consider
            Clean Architecture or CQRS as the project grows.

            ## Endpoints

            - `GET /items` — list all items
            - `GET /items/{id}` — get one item by id
            - `POST /items?name=...` — create an item
            - `DELETE /items/{id}` — delete an item

            ## Getting started

            ```bash
            dotnet restore
            dotnet build
            dotnet run --project src/{{options.ProjectName}}
            ```
            """;

            File.WriteAllText(Path.Combine(solutionDirectory, "README.md"), content);
        }
    }
}
