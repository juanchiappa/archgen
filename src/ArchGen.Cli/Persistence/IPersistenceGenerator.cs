using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Persistence
{
    public interface IPersistenceGenerator
    {
        string Id { get; }

        void GenerateAbstraction(string abstractionDirectory, string abstractionNamespace);

        void GenerateImplementation(
            string implementationDirectory,
            string implementationNamespace,
            string abstractionNamespace,
            string entitiesAssemblyName,
            string entitiesNamespace,
            ProjectOptions options);

        IReadOnlyList<(string PackageId, string Version)> RequiredPackages(ProjectOptions options);
    }
}
