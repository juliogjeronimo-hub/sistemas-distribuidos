using OrderHub.Catalog.Application.Abstractions.Messaging;

namespace OrderHub.Catalog.Application.UseCases.Commands;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price)
    : ICommand
{
    // Parameterless constructor for binding
    public UpdateProductCommand() : this(Guid.Empty, string.Empty, string.Empty, 0) { }
}
