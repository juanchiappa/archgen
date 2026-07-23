using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public sealed class SqliteDapperPersistenceGenerator : IPersistenceGenerator
    {
        public string Id => "sqlite-dapper";

        public IReadOnlyList<(string PackageId, string Version)> RequiredPackages(ProjectOptions options) => new[]
        {
        ("Dapper", "2.1.35"),
        ("Microsoft.Data.Sqlite", "8.0.10"),
        ("SQLitePCLRaw.lib.e_sqlite3", "3.53.3")
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

            var content = $$"""
            using System.Data;
            using System.Reflection;
            using Dapper;
            using Microsoft.Data.Sqlite;
            {{usingLine}}
            namespace {{implementationNamespace}};

            /// <summary>
            /// SQLite + Dapper implementation of IPersistenceProvider. Creates
            /// one table per entity type on first use, with columns inferred
            /// from the entity's public properties.
            /// </summary>
            public sealed class SqlitePersistenceProvider : IPersistenceProvider
            {
                private readonly string _connectionString;

                public SqlitePersistenceProvider(string databasePath = "data.db")
                {
                    _connectionString = $"Data Source={databasePath}";
                }

                private IDbConnection CreateConnection()
                {
                    var connection = new SqliteConnection(_connectionString);
                    connection.Open();
                    return connection;
                }

                private static void EnsureTableExists<T>(IDbConnection connection) where T : class
                {
                    var tableName = typeof(T).Name;
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    var columns = properties.Select(p => p.Name == "Id"
                        ? "Id INTEGER PRIMARY KEY AUTOINCREMENT"
                        : $"{p.Name} TEXT");

                    var sql = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)});";
                    connection.Execute(sql);
                }

                public List<T> GetAll<T>() where T : class
                {
                    using var connection = CreateConnection();
                    EnsureTableExists<T>(connection);
                    return connection.Query<T>($"SELECT * FROM {typeof(T).Name};").ToList();
                }

                public T? GetById<T>(int id) where T : class
                {
                    using var connection = CreateConnection();
                    EnsureTableExists<T>(connection);
                    return connection.QuerySingleOrDefault<T>(
                        $"SELECT * FROM {typeof(T).Name} WHERE Id = @Id;", new { Id = id });
                }

                public void Save<T>(T entity) where T : class
                {
                    using var connection = CreateConnection();
                    EnsureTableExists<T>(connection);

                    var idProperty = typeof(T).GetProperty("Id")
                        ?? throw new InvalidOperationException($"Type {typeof(T).Name} has no 'Id' property.");

                    var id = (int)(idProperty.GetValue(entity) ?? 0);
                    var tableName = typeof(T).Name;
                    var otherProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name != "Id")
                        .ToList();

                    if (id == 0)
                    {
                        var columns = string.Join(", ", otherProperties.Select(p => p.Name));
                        var parameters = string.Join(", ", otherProperties.Select(p => "@" + p.Name));
                        var insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); " +
                                         "SELECT last_insert_rowid();";

                        var newId = connection.ExecuteScalar<int>(insertSql, entity);
                        idProperty.SetValue(entity, newId);
                    }
                    else
                    {
                        var assignments = string.Join(", ", otherProperties.Select(p => $"{p.Name} = @{p.Name}"));
                        var updateSql = $"UPDATE {tableName} SET {assignments} WHERE Id = @Id;";
                        connection.Execute(updateSql, entity);
                    }
                }

                public void Delete<T>(int id) where T : class
                {
                    using var connection = CreateConnection();
                    EnsureTableExists<T>(connection);
                    connection.Execute($"DELETE FROM {typeof(T).Name} WHERE Id = @Id;", new { Id = id });
                }
            }

            """;

            File.WriteAllText(Path.Combine(implementationDirectory, "SqlitePersistenceProvider.cs"), content);
        }
    }
}
