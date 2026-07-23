using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public static class PersistenceProviderNames
    {
        public static string ConcreteClassNameFor(PersistenceKind kind) => kind switch
        {
            PersistenceKind.Json => "JsonPersistenceProvider",
            PersistenceKind.Sqlite => "SqlitePersistenceProvider",
            PersistenceKind.Postgres => "PostgresPersistenceProvider",
            _ => throw new NotSupportedException($"Unknown persistence kind '{kind}'.")
        };
    }
}
