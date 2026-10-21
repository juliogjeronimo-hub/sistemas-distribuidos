using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Abstractions.Messaging;

// Query handlers must be side-effect-free.
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct = default);
}
