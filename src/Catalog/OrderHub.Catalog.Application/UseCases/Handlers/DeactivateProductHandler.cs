using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.Commands;
using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.UseCases.Handlers;

internal sealed class DeactivateProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateProductCommand>
{
    public async Task<Result> HandleAsync(DeactivateProductCommand command, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(command.Id, ct);
        if (product is null)
            return Result.Failure(Error.NotFound(nameof(product), command.Id.ToString()));

        product.Deactivate();

        await products.UpdateAsync(product, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success();
    }
}
