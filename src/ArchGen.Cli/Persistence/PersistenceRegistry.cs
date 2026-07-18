using System;
using ArchGen.Cli.Options;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public static class PersistenceRegistry
    {
        private static readonly Dictionary<PersistenceKind, Func<IPersistenceGenerator>> Factories = new()
        {
            [PersistenceKind.Json] = () => new JsonPersistenceGenerator(),
            // [PersistenceKind.Sqlite] = () => new SqlitePersistenceGenerator(),     // próxima fase
            // [PersistenceKind.Postgres] = () => new PostgresPersistenceGenerator(), // próxima fase
        };

        public static IPersistenceGenerator Resolve(PersistenceKind kind)
        {
            if (!Factories.TryGetValue(kind, out var factory))
            {
                throw new NotSupportedException(
                    $"Persistence backend '{kind}' is not implemented yet. Available backends: " +
                    string.Join(", ", Factories.Keys));
            }

            return factory();
        }
    }
}
