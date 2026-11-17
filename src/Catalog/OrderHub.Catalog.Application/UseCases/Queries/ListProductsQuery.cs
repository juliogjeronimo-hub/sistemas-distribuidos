using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.DTOs;

namespace OrderHub.Catalog.Application.UseCases.Queries;

public sealed record ListProductsQuery(
    int Page,
    int PageSize,
    bool IncludeInactive = false)
    : IQuery<PaginatedResponse<ProductResponse>>;
