using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Abstractions.Messaging;

/// <summary>
/// Handles a command that produces no typed return value (fire-and-confirm pattern).
/// </summary>
/// <typeparam name="TCommand">The command type. Must implement <see cref="ICommand"/>.</typeparam>
public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>
/// Handles a command that produces a typed value on success.
/// </summary>
/// <typeparam name="TCommand">The command type. Must implement <see cref="ICommand{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the success value.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default);
}
