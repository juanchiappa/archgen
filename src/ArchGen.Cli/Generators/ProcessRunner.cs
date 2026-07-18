using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ArchGen.Cli.Generators
{
    public static class ProcessRunner
    {
        public static void RunDotnet(string arguments, string workingDirectory)
            => Run("dotnet", arguments, workingDirectory);

        public static void Run(string fileName, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start the '{fileName}' process.");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"'{fileName} {arguments}' failed with exit code {process.ExitCode}.\n{stdout}\n{stderr}");
            }
        }

    }
}
