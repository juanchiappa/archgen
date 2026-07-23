using System;
using ArchGen.Cli.Options;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public static class PersistenceRegistry
    {
        private static readonly Dictionary<(PersistenceKind, OrmKind), Func<IPersistenceGenerator>> OrmAwareFactories = new()
        {
            [(PersistenceKind.Sqlite, OrmKind.EfCore)] = () => new SqliteEfCorePersistenceGenerator(),
            [(PersistenceKind.Sqlite, OrmKind.Dapper)] = () => new SqliteDapperPersistenceGenerator(),
            [(PersistenceKind.Postgres, OrmKind.EfCore)] = () => new PostgresEfCorePersistenceGenerator(),
            [(PersistenceKind.Postgres, OrmKind.Dapper)] = () => new PostgresDapperPersistenceGenerator(),
        };

        public static IPersistenceGenerator Resolve(ProjectOptions options)
        {
            if (options.Persistence == PersistenceKind.Json)
            {
                return new JsonPersistenceGenerator();
            }

            if (!OrmAwareFactories.TryGetValue((options.Persistence, options.Orm), out var factory))
            {
                throw new NotSupportedException(
                    $"No persistence generator registered for {options.Persistence} + {options.Orm}.");
            }

            return factory();
        }
    }
}
