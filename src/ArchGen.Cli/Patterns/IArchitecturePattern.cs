using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public interface IArchitecturePattern
    {
        string Id { get; }
        string DisplayName { get; }
        void Generate(ProjectOptions options);
    }
}
