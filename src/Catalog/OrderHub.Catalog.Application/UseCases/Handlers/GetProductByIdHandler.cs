using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.DTOs;
using OrderHub.Catalog.Application.UseCases.Queries;
using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;

namespace OrderHub.Catalog.Application.UseCases.Handlers;

internal sealed class GetProductByIdHandler(
    IProductRepository products)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(GetProductByIdQuery query, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(query.Id, ct);
        if (product is null)
            return Result.Fail<ProductResponse>(Error.NotFound(nameof(product), query.Id.ToString()));

        var response = new ProductResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price.Amount,
            Money.Currency,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);

        return Result.Ok(response);
    }
}
