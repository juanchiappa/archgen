using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Options
{
    public enum ArchitecturePattern
    {
        NTier,
        CleanArchitecture,
        Cqrs,
        MinimalApi
    }
    public enum PersistenceKind
    {
        Json,
        Sqlite,
        Postgres
    }
    public enum OrmKind
    {
        EfCore,
        Dapper
    }
    public enum UiKind
    {
        Console,
        WinForms,
        Wpf,
        Blazor,
        Api
    }
    public sealed class ProjectOptions
    {
        public required string ProjectName { get; init; }
        public required string OutputDirectory { get; init; }

        public required ArchitecturePattern Pattern { get; init; }
        public required PersistenceKind Persistence { get; init; }

        public OrmKind Orm { get; init; } = OrmKind.EfCore;
        public required UiKind Ui { get; init; }
    }
}
