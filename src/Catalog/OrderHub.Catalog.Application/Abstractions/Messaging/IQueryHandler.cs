using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Abstractions.Messaging;

/// <summary>
/// Handles a query and returns a typed result. Query handlers must be side-effect-free.
/// </summary>
/// <typeparam name="TQuery">The query type. Must implement <see cref="IQuery{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the success value.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct = default);
}
