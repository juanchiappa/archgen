using ArchGen.Cli.Options;

namespace ArchGen.Cli.Persistence;

public sealed class JsonPersistenceGenerator : IPersistenceGenerator
{
    public string Id => "json";

    public IReadOnlyList<(string PackageId, string Version)> RequiredPackages(ProjectOptions options)
        => Array.Empty<(string, string)>();

    public void GenerateAbstraction(string abstractionDirectory, string abstractionNamespace)
    {
        File.WriteAllText(
            Path.Combine(abstractionDirectory, "IPersistenceProvider.cs"),
            BuildInterfaceFile(abstractionNamespace));
    }

    public void GenerateImplementation(
        string implementationDirectory,
        string implementationNamespace,
        string abstractionNamespace,
        string entitiesAssemblyName,
        string entitiesNamespace,
        ProjectOptions options)
    {
        File.WriteAllText(
            Path.Combine(implementationDirectory, "JsonPersistenceProvider.cs"),
            BuildJsonProviderFile(implementationNamespace, abstractionNamespace));
    }

    private static string BuildInterfaceFile(string abstractionNamespace) => $$"""
        namespace {{abstractionNamespace}};

        /// <summary>
        /// Storage-agnostic persistence contract. Business logic depends
        /// only on this interface, never on a concrete backend.
        /// </summary>
        public interface IPersistenceProvider
        {
            List<T> GetAll<T>() where T : class;
            T? GetById<T>(int id) where T : class;
            void Save<T>(T entity) where T : class;
            void Delete<T>(int id) where T : class;
        }

        """;

    private static string BuildJsonProviderFile(string implementationNamespace, string abstractionNamespace)
    {
        var usingLine = implementationNamespace == abstractionNamespace
            ? ""
            : $"using {abstractionNamespace};\n";

        return $$"""
            using System.Text.Json;
            {{usingLine}}
            namespace {{implementationNamespace}};

            /// <summary>
            /// JSON-file-backed implementation of IPersistenceProvider. Stores
            /// one JSON file per entity type under DataDirectory. Intended for
            /// local/personal projects, not for concurrent multi-user access.
            /// </summary>
            public sealed class JsonPersistenceProvider : IPersistenceProvider
            {
                private static readonly JsonSerializerOptions SerializerOptions = new()
                {
                    WriteIndented = true
                };

                public string DataDirectory { get; }

                public JsonPersistenceProvider(string dataDirectory = "data")
                {
                    DataDirectory = dataDirectory;
                    Directory.CreateDirectory(DataDirectory);
                }

                public List<T> GetAll<T>() where T : class
                {
                    var path = FilePathFor<T>();
                    if (!File.Exists(path))
                    {
                        return new List<T>();
                    }

                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
                }

                public T? GetById<T>(int id) where T : class
                {
                    var all = GetAll<T>();
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty is null)
                    {
                        throw new InvalidOperationException($"Type {typeof(T).Name} has no 'Id' property.");
                    }

                    return all.FirstOrDefault(item => Equals(idProperty.GetValue(item), id));
                }

                public void Save<T>(T entity) where T : class
                {
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty is null)
                    {
                        throw new InvalidOperationException($"Type {typeof(T).Name} has no 'Id' property.");
                    }

                    var all = GetAll<T>();
                    var id = (int)(idProperty.GetValue(entity) ?? 0);

                    var existingIndex = all.FindIndex(item => Equals(idProperty.GetValue(item), id));
                    if (id == 0)
                    {
                        var nextId = all.Count == 0 ? 1 : all.Max(item => (int)(idProperty.GetValue(item) ?? 0)) + 1;
                        idProperty.SetValue(entity, nextId);
                        all.Add(entity);
                    }
                    else if (existingIndex >= 0)
                    {
                        all[existingIndex] = entity;
                    }
                    else
                    {
                        all.Add(entity);
                    }

                    WriteAll(all);
                }

                public void Delete<T>(int id) where T : class
                {
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty is null)
                    {
                        throw new InvalidOperationException($"Type {typeof(T).Name} has no 'Id' property.");
                    }

                    var all = GetAll<T>();
                    all.RemoveAll(item => Equals(idProperty.GetValue(item), id));
                    WriteAll(all);
                }

                private void WriteAll<T>(List<T> items) where T : class
                {
                    var json = JsonSerializer.Serialize(items, SerializerOptions);
                    File.WriteAllText(FilePathFor<T>(), json);
                }

                private string FilePathFor<T>() => Path.Combine(DataDirectory, $"{typeof(T).Name}.json");
            }
            """;
    }
}