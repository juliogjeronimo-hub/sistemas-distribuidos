using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.DTOs;

namespace OrderHub.Catalog.Application.UseCases.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
