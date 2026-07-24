using System;
using System.Collections.Generic;
using System.Text;

namespace ArchGen.Cli.Patterns.Cqrs
{
    public static class MediatorTemplates
    {
        public static string BuildMediatorContracts(string domainNamespace) => $$"""
        namespace {{domainNamespace}};

        public interface ICommand<TResult> { }

        public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
        {
            Task<TResult> Handle(TCommand command);
        }

        public interface IQuery<TResult> { }

        public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
        {
            Task<TResult> Handle(TQuery query);
        }

        /// <summary>
        /// Dispatches a command or query to its registered handler. UI code
        /// depends only on this interface, never on concrete handler classes —
        /// that's the core of what makes CQRS decoupled.
        /// </summary>
        public interface IMediator
        {
            Task<TResult> Send<TResult>(ICommand<TResult> command);
            Task<TResult> Send<TResult>(IQuery<TResult> query);
        }

        """;

        public static string BuildMediatorImplementation(string domainNamespace, string implementationNamespace)
        {
            var usingLine = domainNamespace == implementationNamespace ? "" : $"using {domainNamespace};\n";

            return $$"""
            using Microsoft.Extensions.DependencyInjection;
            {{usingLine}}
            namespace {{implementationNamespace}};

            /// <summary>
            /// Resolves the handler for a given command/query type from the DI
            /// container by reflection, then invokes it. Simple by design: no
            /// pipeline behaviors/middleware, just dispatch — enough for a
            /// scaffolded starting point.
            /// </summary>
            public sealed class Mediator : IMediator
            {
                private readonly IServiceProvider _serviceProvider;

                public Mediator(IServiceProvider serviceProvider)
                {
                    _serviceProvider = serviceProvider;
                }

                public Task<TResult> Send<TResult>(ICommand<TResult> command)
                {
                    var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
                    dynamic handler = _serviceProvider.GetRequiredService(handlerType);
                    return handler.Handle((dynamic)command);
                }

                public Task<TResult> Send<TResult>(IQuery<TResult> query)
                {
                    var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
                    dynamic handler = _serviceProvider.GetRequiredService(handlerType);
                    return handler.Handle((dynamic)query);
                }
            }

            """;
        }
    }
}
