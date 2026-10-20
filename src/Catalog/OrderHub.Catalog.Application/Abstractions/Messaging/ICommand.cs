using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for commands that produce no return value beyond success/failure.
/// </summary>
public interface ICommand;

/// <summary>
/// Marker interface for commands that produce a typed response on success.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned when the command succeeds.</typeparam>
public interface ICommand<TResponse>;
