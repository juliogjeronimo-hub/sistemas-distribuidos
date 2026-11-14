namespace OrderHub.Catalog.Application.UseCases.DTOs;

public sealed record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
