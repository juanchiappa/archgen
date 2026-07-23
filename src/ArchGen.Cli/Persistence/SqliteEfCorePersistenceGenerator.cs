using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public sealed class SqliteEfCorePersistenceGenerator : IPersistenceGenerator
    {
        public string Id => "sqlite-efcore";

        public IReadOnlyList<(string PackageId, string Version)> RequiredPackages(ProjectOptions options) => new[]
        {
        ("Microsoft.EntityFrameworkCore.Sqlite", "8.0.10")
    };

        public void GenerateAbstraction(string abstractionDirectory, string abstractionNamespace)
        {
            File.WriteAllText(
                Path.Combine(abstractionDirectory, "IPersistenceProvider.cs"),
                $$"""
            namespace {{abstractionNamespace}};

            /// <summary>
            /// Storage-agnostic persistence contract. Business logic depends
            /// only on this interface, never on a concrete backend.
            /// </summary>
            public interface IPersistenceProvider
            {
                List<T> GetAll<T>() where T : class;
                T? GetById<T>(int id) where T : class;
                void Save<T>(T entity) where T : class;
                void Delete<T>(int id) where T : class;
            }

            """);
        }

        public void GenerateImplementation(
            string implementationDirectory,
            string implementationNamespace,
            string abstractionNamespace,
            string entitiesAssemblyName,
            string entitiesNamespace,
            ProjectOptions options)
        {
            var usingLine = implementationNamespace == abstractionNamespace
                ? ""
                : $"using {abstractionNamespace};\n";

            var dbContextContent = $$"""
            using System.Reflection;
            using Microsoft.EntityFrameworkCore;
            {{usingLine}}
            namespace {{implementationNamespace}};

            /// <summary>
            /// EF Core DbContext that discovers entity types by reflection over
            /// the "{{entitiesNamespace}}" namespace in the "{{entitiesAssemblyName}}"
            /// assembly. This means entities added later are registered
            /// automatically — no need to touch this file when the domain grows.
            /// </summary>
            public sealed class ArchGenDbContext : DbContext
            {
                private readonly string _connectionString;

                public ArchGenDbContext(string databasePath = "data.db")
                {
                    _connectionString = $"Data Source={databasePath}";
                }

                protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                    => optionsBuilder.UseSqlite(_connectionString);

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    var entityTypes = Assembly.Load("{{entitiesAssemblyName}}").GetTypes()
                        .Where(type => type.Namespace == "{{entitiesNamespace}}" && type.IsClass && !type.IsAbstract);

                    foreach (var entityType in entityTypes)
                    {
                        modelBuilder.Entity(entityType);
                    }
                }
            }

            """;

            var providerContent = $$"""
            {{usingLine}}
            namespace {{implementationNamespace}};

            /// <summary>
            /// EF Core (SQLite) implementation of IPersistenceProvider, backed by
            /// ArchGenDbContext.
            /// </summary>
            public sealed class SqlitePersistenceProvider : IPersistenceProvider
            {
                private readonly string _databasePath;

                public SqlitePersistenceProvider(string databasePath = "data.db")
                {
                    _databasePath = databasePath;
                }

                public List<T> GetAll<T>() where T : class
                {
                    using var context = new ArchGenDbContext(_databasePath);
                    context.Database.EnsureCreated();
                    return context.Set<T>().ToList();
                }

                public T? GetById<T>(int id) where T : class
                {
                    using var context = new ArchGenDbContext(_databasePath);
                    context.Database.EnsureCreated();
                    return context.Set<T>().Find(id);
                }

                public void Save<T>(T entity) where T : class
                {
                    using var context = new ArchGenDbContext(_databasePath);
                    context.Database.EnsureCreated();

                    var idProperty = typeof(T).GetProperty("Id")
                        ?? throw new InvalidOperationException($"Type {typeof(T).Name} has no 'Id' property.");

                    var id = (int)(idProperty.GetValue(entity) ?? 0);

                    if (id == 0)
                    {
                        context.Set<T>().Add(entity);
                    }
                    else
                    {
                        context.Set<T>().Update(entity);
                    }

                    context.SaveChanges();
                }

                public void Delete<T>(int id) where T : class
                {
                    using var context = new ArchGenDbContext(_databasePath);
                    context.Database.EnsureCreated();

                    var entity = context.Set<T>().Find(id);
                    if (entity is not null)
                    {
                        context.Set<T>().Remove(entity);
                        context.SaveChanges();
                    }
                }
            }

            """;

            File.WriteAllText(Path.Combine(implementationDirectory, "ArchGenDbContext.cs"), dbContextContent);
            File.WriteAllText(Path.Combine(implementationDirectory, "SqlitePersistenceProvider.cs"), providerContent);
        }
    }
}
