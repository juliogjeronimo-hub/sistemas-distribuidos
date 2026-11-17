namespace OrderHub.Catalog.Application.UseCases.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
