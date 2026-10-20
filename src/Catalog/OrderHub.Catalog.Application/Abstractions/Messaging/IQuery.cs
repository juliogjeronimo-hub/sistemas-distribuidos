namespace OrderHub.Catalog.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for queries that return a typed response.
/// Queries must be side-effect-free reads.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned by the query.</typeparam>
public interface IQuery<TResponse>;
