namespace OrderHub.BuildingBlocks.Messaging;

/// <summary>
/// Marker for integration events that cross service boundaries.
/// Concrete events live as immutable records in <c>OrderHub.BuildingBlocks.Contracts</c>.
/// </summary>
public interface IIntegrationEvent;
