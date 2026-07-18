using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public interface IPersistenceGenerator
    {
        string Id { get; }
        void Generate(string targetProjectDirectory, string rootNamespace, ProjectOptions options);
        IReadOnlyList<(string PackageId, string Version)> RequiredPackages(ProjectOptions options);
    }
}
