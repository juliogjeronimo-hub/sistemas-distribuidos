namespace OrderHub.Catalog.Application.Abstractions.Messaging;

// Queries must be side-effect-free reads.
public interface IQuery<TResponse>;
