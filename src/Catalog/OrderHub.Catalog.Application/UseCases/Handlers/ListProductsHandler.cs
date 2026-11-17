using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.DTOs;
using OrderHub.Catalog.Application.UseCases.Queries;
using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;

namespace OrderHub.Catalog.Application.UseCases.Handlers;

internal sealed class ListProductsHandler(
    IProductRepository products,
    ICurrentUser currentUser)
    : IQueryHandler<ListProductsQuery, PaginatedResponse<ProductResponse>>
{
    public async Task<Result<PaginatedResponse<ProductResponse>>> HandleAsync(
        ListProductsQuery query,
        CancellationToken ct = default)
    {
        var isAdmin = currentUser.Roles.Contains("admin");
        var includeInactive = query.IncludeInactive && isAdmin;

        var allProducts = await products.ListAsync(includeInactive, ct);

        var totalCount = allProducts.Count;
        var skip = (query.Page - 1) * query.PageSize;
        var items = allProducts
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(p => new ProductResponse(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Price.Amount,
                Money.Currency,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt))
            .ToList();

        var response = new PaginatedResponse<ProductResponse>(
            items,
            query.Page,
            query.PageSize,
            totalCount);

        return Result.Ok(response);
    }
}
