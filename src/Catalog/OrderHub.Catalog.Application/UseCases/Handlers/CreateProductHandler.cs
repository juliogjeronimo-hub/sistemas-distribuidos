using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.Commands;
using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;

namespace OrderHub.Catalog.Application.UseCases.Handlers;

internal sealed class CreateProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var money = Money.Create(command.Price);
        if (money.IsFailure)
            return Result.Fail<Guid>(money.Error);

        var existingSku = await products.GetBySkuAsync(command.Sku, ct);
        if (existingSku is not null)
            return Result.Fail<Guid>(Error.Validation(
                nameof(command.Sku),
                $"A product with SKU '{command.Sku}' already exists."));

        var product = Product.Create(
            command.Sku,
            command.Name,
            command.Description,
            money.Value,
            timeProvider);

        if (product.IsFailure)
            return Result.Fail<Guid>(product.Error);

        await products.AddAsync(product.Value, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Ok(product.Value.Id);
    }
}
