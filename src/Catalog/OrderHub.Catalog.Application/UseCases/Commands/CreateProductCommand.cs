using OrderHub.Catalog.Application.Abstractions.Messaging;

namespace OrderHub.Catalog.Application.UseCases.Commands;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    decimal Price)
    : ICommand<Guid>;
