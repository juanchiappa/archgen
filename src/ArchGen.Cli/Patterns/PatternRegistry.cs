using ArchGen.Cli.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public static class PatternRegistry
    {
        private static readonly Dictionary<ArchitecturePattern, Func<IArchitecturePattern>> Factories = new()
        {
            [ArchitecturePattern.NTier] = () => new NTierPattern(),
            [ArchitecturePattern.CleanArchitecture] = () => new CleanArchitecturePattern(),
        };
        public static IArchitecturePattern Resolve(ArchitecturePattern pattern)
        {
            if (!Factories.TryGetValue(pattern, out var factory))
            {
                throw new NotSupportedException(
                    $"Pattern '{pattern}' is not implemented yet. Available patterns: " +
                    string.Join(", ", Factories.Keys));
            }

            return factory();
        }
    }
}
