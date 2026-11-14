using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.Commands;
using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;

namespace OrderHub.Catalog.Application.UseCases.Handlers;

internal sealed class UpdateProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> HandleAsync(UpdateProductCommand command, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(command.Id, ct);
        if (product is null)
            return Result.Failure(Error.NotFound(nameof(product), command.Id.ToString()));

        var money = Money.Create(command.Price);
        if (money.IsFailure)
            return Result.Failure(money.Error);

        var updateResult = product.Update(
            command.Name,
            command.Description,
            money.Value,
            timeProvider);

        if (updateResult.IsFailure)
            return updateResult;

        await products.UpdateAsync(product, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success();
    }
}
