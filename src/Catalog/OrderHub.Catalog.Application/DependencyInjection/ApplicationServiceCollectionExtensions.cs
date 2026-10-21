using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.Decorators;

namespace OrderHub.Catalog.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        // Scrutor applies decorators in reverse registration order, so the
        // effective call order is Validation -> Logging -> Handler.
        services.TryDecorate(typeof(ICommandHandler<>),
            typeof(LoggingCommandHandlerDecorator<>));

        services.TryDecorate(typeof(ICommandHandler<>),
            typeof(ValidationCommandHandlerDecorator<>));

        services.TryDecorate(typeof(ICommandHandler<,>),
            typeof(LoggingCommandHandlerDecorator<,>));

        services.TryDecorate(typeof(ICommandHandler<,>),
            typeof(ValidationCommandHandlerDecorator<,>));

        return services;
    }
}
