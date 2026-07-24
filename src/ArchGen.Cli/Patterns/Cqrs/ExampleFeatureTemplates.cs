using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns.Cqrs
{
    public static class ExampleFeatureTemplates
    {
        public static string BuildExampleEntity(string domainNamespace) => $$"""
        namespace {{domainNamespace}};

        /// <summary>
        /// Example entity demonstrating the CQRS vertical slice. Replace or
        /// remove once you add your own entities.
        /// </summary>
        public class ExampleItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        """;

        public static string BuildCreateCommand(string domainNamespace, string applicationNamespace)
        {
            var usingLine = domainNamespace == applicationNamespace ? "" : $"using {domainNamespace};\n";

            return $$"""
            {{usingLine}}
            namespace {{applicationNamespace}}.Commands;

            public sealed class CreateExampleItemCommand : ICommand<int>
            {
                public required string Name { get; init; }
            }

            public sealed class CreateExampleItemCommandHandler : ICommandHandler<CreateExampleItemCommand, int>
            {
                private readonly IPersistenceProvider _persistence;

                public CreateExampleItemCommandHandler(IPersistenceProvider persistence)
                {
                    _persistence = persistence;
                }

                public Task<int> Handle(CreateExampleItemCommand command)
                {
                    var item = new ExampleItem { Name = command.Name };
                    _persistence.Save(item);
                    return Task.FromResult(item.Id);
                }
            }

            """;
        }

        public static string BuildGetAllQuery(string domainNamespace, string applicationNamespace)
        {
            var usingLine = domainNamespace == applicationNamespace ? "" : $"using {domainNamespace};\n";

            return $$"""
            {{usingLine}}
            namespace {{applicationNamespace}}.Queries;

            public sealed class GetAllExampleItemsQuery : IQuery<List<ExampleItem>>
            {
            }

            public sealed class GetAllExampleItemsQueryHandler
                : IQueryHandler<GetAllExampleItemsQuery, List<ExampleItem>>
            {
                private readonly IPersistenceProvider _persistence;

                public GetAllExampleItemsQueryHandler(IPersistenceProvider persistence)
                {
                    _persistence = persistence;
                }

                public Task<List<ExampleItem>> Handle(GetAllExampleItemsQuery query)
                    => Task.FromResult(_persistence.GetAll<ExampleItem>());
            }

            """;
        }

    }
}
