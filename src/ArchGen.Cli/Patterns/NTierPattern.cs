using ArchGen.Cli.Generators;
using ArchGen.Cli.Options;
using ArchGen.Cli.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns
{
    public sealed class NTierPattern : IArchitecturePattern
    {
        public string Id => "ntier";
        public string DisplayName => "N-Tier";

        public void Generate(ProjectOptions options)
        {
            var solutionDirectory = SolutionGenerator.CreateSolution(options);

            var entitiesProjectName = $"{options.ProjectName}.Entities";
            var dataAccessProjectName = $"{options.ProjectName}.DataAccess";
            var businessLogicProjectName = $"{options.ProjectName}.BusinessLogic";

            var entitiesDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, entitiesProjectName);
            var dataAccessDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, dataAccessProjectName);
            var businessLogicDir = SolutionGenerator.CreateClassLibrary(solutionDirectory, businessLogicProjectName);

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"),
                Path.Combine(entitiesDir, $"{entitiesProjectName}.csproj"));

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(businessLogicDir, $"{businessLogicProjectName}.csproj"),
                Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"));

            var uiDir = GenerateUiProject(solutionDirectory, businessLogicDir, businessLogicProjectName, options);

            var dataAccessNamespace = $"{options.ProjectName}.DataAccess";
            var persistenceGenerator = PersistenceRegistry.Resolve(options);
            var entitiesAssemblyName = entitiesProjectName; // "{ProjectName}.Entities"
            var entitiesNamespace = entitiesProjectName;

            persistenceGenerator.GenerateAbstraction(dataAccessDir, dataAccessNamespace);
            persistenceGenerator.GenerateImplementation(
                dataAccessDir, dataAccessNamespace, dataAccessNamespace,
                entitiesAssemblyName, entitiesNamespace, options);

            if (options.Ui == UiKind.Api)
            {
                GenerateApiHost(uiDir, dataAccessNamespace, options);
            }
            else if (options.Ui == UiKind.WinForms)
            {
                GenerateWinFormsHost(solutionDirectory, uiDir, dataAccessNamespace, options);
            }
            else if (options.Ui == UiKind.Wpf)
            {
                GenerateWpfHost(solutionDirectory, uiDir, dataAccessNamespace, options);
            }
            foreach (var (packageId, version) in persistenceGenerator.RequiredPackages(options))
            {
                SolutionGenerator.AddPackage(
                    solutionDirectory,
                    Path.Combine(dataAccessDir, $"{dataAccessProjectName}.csproj"),
                    packageId,
                    version);
            }

            WriteReadme(solutionDirectory, options);
        }

        private static string GenerateUiProject(
            string solutionDirectory,
            string businessLogicDir,
            string businessLogicProjectName,
            ProjectOptions options)
        {
            var uiProjectName = $"{options.ProjectName}.UI";

            string uiDir = options.Ui switch
            {
                UiKind.Console => SolutionGenerator.CreateConsoleProject(solutionDirectory, uiProjectName),
                UiKind.Api => SolutionGenerator.CreateWebApiProject(solutionDirectory, uiProjectName),
                UiKind.WinForms => SolutionGenerator.CreateWinFormsProject(solutionDirectory, uiProjectName),
                UiKind.Wpf => SolutionGenerator.CreateWpfProject(solutionDirectory, uiProjectName),
                _ => throw new NotSupportedException($"UI type '{options.Ui}' is not implemented yet (planned for a later phase). " +
                    "Use --ui console or --ui api for now.")
            };

            SolutionGenerator.AddProjectReference(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiProjectName}.csproj"),
                Path.Combine(businessLogicDir, $"{businessLogicProjectName}.csproj"));

            return uiDir;
        }

        private static void WriteReadme(string solutionDirectory, ProjectOptions options)
        {
            var content = $"""
            # {options.ProjectName}

            Generated with archgen using the **N-Tier** architecture pattern.

            ## Layers

            - `{options.ProjectName}.Entities` — plain domain entities, no dependencies.
            - `{options.ProjectName}.DataAccess` — persistence layer ({options.Persistence}).
            - `{options.ProjectName}.BusinessLogic` — application/business rules, depends on DataAccess.
            - `{options.ProjectName}.UI` — {options.Ui} entry point, depends on BusinessLogic.

            ## Getting started

            ```bash
            dotnet restore
            dotnet build
            dotnet run --project src/{options.ProjectName}.UI
            ```
            """;

            File.WriteAllText(Path.Combine(solutionDirectory, "README.md"), content);
        }

        private static void GenerateApiHost(string uiDir, string dataAccessNamespace, ProjectOptions options)
        {
            var concreteClassName = Persistence.PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);

            var content = $$"""
                using {{dataAccessNamespace}};

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();

                var app = builder.Build();

                app.MapGet("/", () => "{{options.ProjectName}} API is running.");

                app.Run();
                """;

            File.WriteAllText(Path.Combine(uiDir, "Program.cs"), content);
        }

        private static void GenerateWinFormsHost(
    string solutionDirectory, string uiDir, string dataAccessNamespace, ProjectOptions options)
        {
            var uiProjectName = $"{options.ProjectName}.UI";
            var uiNamespace = uiProjectName; // dotnet new usa el nombre del proyecto como namespace raíz
            var concreteClassName = Persistence.PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);

            var programContent = $$"""
                using Microsoft.Extensions.DependencyInjection;
                using {{dataAccessNamespace}};

                namespace {{uiNamespace}};

                internal static class Program
                {
                    [STAThread]
                    static void Main()
                    {
                        var services = new ServiceCollection();
                        services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();
                        using var serviceProvider = services.BuildServiceProvider();
                        var persistenceProvider = serviceProvider.GetRequiredService<IPersistenceProvider>();

                        ApplicationConfiguration.Initialize();
                        Application.Run(new Form1(persistenceProvider));
                    }
                }
                """;

            var form1Content = $$"""
                using {{dataAccessNamespace}};

                namespace {{uiNamespace}};

                public partial class Form1 : Form
                {
                    public Form1() : this(null!)
                    {
                    }

                    public Form1(IPersistenceProvider persistenceProvider)
                    {
                        InitializeComponent();
                        Text = "{{options.ProjectName}}";

                        var label = new Label
                        {
                            AutoSize = true,
                            Location = new Point(20, 20),
                            Text = persistenceProvider is null
                                ? "No persistence provider resolved."
                                : $"Persistence provider resolved via DI: {persistenceProvider.GetType().Name}"
                        };

                        Controls.Add(label);
                    }
                }
                """;

            File.WriteAllText(Path.Combine(uiDir, "Program.cs"), programContent);
            File.WriteAllText(Path.Combine(uiDir, "Form1.cs"), form1Content);

            SolutionGenerator.AddPackage(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiProjectName}.csproj"),
                "Microsoft.Extensions.DependencyInjection",
                "8.0.0");
        }

        private static void GenerateWpfHost(
    string solutionDirectory, string uiDir, string dataAccessNamespace, ProjectOptions options)
        {
            var uiProjectName = $"{options.ProjectName}.UI";
            var uiNamespace = uiProjectName;
            var concreteClassName = Persistence.PersistenceProviderNames.ConcreteClassNameFor(options.Persistence);

            var appXamlContent = $"""
                <Application x:Class="{uiNamespace}.App"
                             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                </Application>
                """;

            var appXamlCsContent = $$"""
                using System.Windows;
                using Microsoft.Extensions.DependencyInjection;
                using {{dataAccessNamespace}};

                namespace {{uiNamespace}};

                public partial class App : Application
                {
                    protected override void OnStartup(StartupEventArgs e)
                    {
                        base.OnStartup(e);

                        var services = new ServiceCollection();
                        services.AddSingleton<IPersistenceProvider, {{concreteClassName}}>();
                        using var serviceProvider = services.BuildServiceProvider();
                        var persistenceProvider = serviceProvider.GetRequiredService<IPersistenceProvider>();

                        new MainWindow(persistenceProvider).Show();
                    }
                }
                """;

            var mainWindowXamlCsContent = $$"""
                using System.Windows;
                using System.Windows.Controls;
                using {{dataAccessNamespace}};

                namespace {{uiNamespace}};

                public partial class MainWindow : Window
                {
                    public MainWindow() : this(null!)
                    {
                    }

                    public MainWindow(IPersistenceProvider persistenceProvider)
                    {
                        InitializeComponent();
                        Title = "{{options.ProjectName}}";

                        Content = new TextBlock
                        {
                            Margin = new Thickness(20),
                            Text = persistenceProvider is null
                                ? "No persistence provider resolved."
                                : $"Persistence provider resolved via DI: {persistenceProvider.GetType().Name}"
                        };
                    }
                }
                """;

            File.WriteAllText(Path.Combine(uiDir, "App.xaml"), appXamlContent);
            File.WriteAllText(Path.Combine(uiDir, "App.xaml.cs"), appXamlCsContent);
            File.WriteAllText(Path.Combine(uiDir, "MainWindow.xaml.cs"), mainWindowXamlCsContent);

            SolutionGenerator.AddPackage(
                solutionDirectory,
                Path.Combine(uiDir, $"{uiProjectName}.csproj"),
                "Microsoft.Extensions.DependencyInjection",
                "8.0.0");
        }
    }
}
