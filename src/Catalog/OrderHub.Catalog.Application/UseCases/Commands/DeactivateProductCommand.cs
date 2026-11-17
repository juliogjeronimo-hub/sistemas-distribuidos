using OrderHub.Catalog.Application.Abstractions.Messaging;

namespace OrderHub.Catalog.Application.UseCases.Commands;

public sealed record DeactivateProductCommand(Guid Id) : ICommand;
