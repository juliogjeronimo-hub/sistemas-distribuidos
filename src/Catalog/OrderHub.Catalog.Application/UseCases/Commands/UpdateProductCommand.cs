using OrderHub.Catalog.Application.Abstractions.Messaging;

namespace OrderHub.Catalog.Application.UseCases.Commands;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price)
    : ICommand;
