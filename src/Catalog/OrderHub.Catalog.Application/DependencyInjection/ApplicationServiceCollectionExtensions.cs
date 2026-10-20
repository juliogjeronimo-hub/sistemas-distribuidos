using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.Decorators;

namespace OrderHub.Catalog.Application.DependencyInjection;

/// <summary>
/// Composition root for the Application layer.
/// Called once from the Api's Program.cs — keeps DI wiring out of individual handlers.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Application-layer services:
    /// <list type="bullet">
    ///   <item>All <see cref="ICommandHandler{TCommand}"/> and <see cref="IQueryHandler{TQuery,TResponse}"/> implementations found in this assembly.</item>
    ///   <item>All <see cref="FluentValidation.IValidator{T}"/> implementations found in this assembly.</item>
    ///   <item>Decorator chain (Validation → Logging) applied to every <see cref="ICommandHandler{TCommand}"/> via Scrutor.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        // ── Validators ────────────────────────────────────────────────────────
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // ── Handlers (scan) ──────────────────────────────────────────────────
        // Scrutor scans the assembly and registers every concrete implementation
        // of ICommandHandler<> and IQueryHandler<> as its interface (Scoped).
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

        // ── Decorator chain: Validation → Logging ─────────────────────────────
        // Scrutor applies decorators in reverse registration order:
        //   actual call order = ValidationDecorator → LoggingDecorator → Handler
        //
        // Non-generic variant (ICommandHandler<TCommand>)
        services.Decorate(typeof(ICommandHandler<>),
            typeof(LoggingCommandHandlerDecorator<>));

        services.Decorate(typeof(ICommandHandler<>),
            typeof(ValidationCommandHandlerDecorator<>));

        // Typed variant (ICommandHandler<TCommand, TResponse>)
        services.Decorate(typeof(ICommandHandler<,>),
            typeof(LoggingCommandHandlerDecorator<,>));

        services.Decorate(typeof(ICommandHandler<,>),
            typeof(ValidationCommandHandlerDecorator<,>));

        return services;
    }
}
