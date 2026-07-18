using System;
using System.Collections.Generic;
using System.Text;
using ArchGen.Cli.Options;

namespace ArchGen.Cli.Generators
{
    public static class SolutionGenerator
    {
        public static string CreateSolution(ProjectOptions options)
        {
            var solutionDirectory = Path.Combine(options.OutputDirectory, options.ProjectName);
            Directory.CreateDirectory(solutionDirectory);

            ProcessRunner.RunDotnet($"new sln -n \"{options.ProjectName}\"", solutionDirectory);

            return solutionDirectory;
        }

        public static string CreateClassLibrary(string solutionDirectory, string projectName)
        {
            var projectDirectory = Path.Combine(solutionDirectory, "src", projectName);
            ProcessRunner.RunDotnet(
                $"new classlib -n \"{projectName}\" -o \"{projectDirectory}\"",
                solutionDirectory);

            var stub = Path.Combine(projectDirectory, "Class1.cs");
            if (File.Exists(stub))
            {
                File.Delete(stub);
            }

            AddToSolution(solutionDirectory, Path.Combine(projectDirectory, $"{projectName}.csproj"));
            return projectDirectory;
        }

        public static string CreateConsoleProject(string solutionDirectory, string projectName)
        {
            var projectDirectory = Path.Combine(solutionDirectory, "src", projectName);
            ProcessRunner.RunDotnet(
                $"new console -n \"{projectName}\" -o \"{projectDirectory}\" --use-program-main false",
                solutionDirectory);

            AddToSolution(solutionDirectory, Path.Combine(projectDirectory, $"{projectName}.csproj"));
            return projectDirectory;
        }

        public static void AddToSolution(string solutionDirectory, string csprojPath)
    => ProcessRunner.RunDotnet($"sln add \"{csprojPath}\"", solutionDirectory);

        public static void AddProjectReference(string solutionDirectory, string fromCsproj, string toCsproj)
    => ProcessRunner.RunDotnet($"add \"{fromCsproj}\" reference \"{toCsproj}\"", solutionDirectory);

        public static void AddPackage(string solutionDirectory, string csprojPath, string packageId, string version)
    => ProcessRunner.RunDotnet($"add \"{csprojPath}\" package {packageId} --version {version}", solutionDirectory);
    }
}
