using OrderHub.BuildingBlocks.Contracts;

namespace OrderHub.BuildingBlocks.Messaging;

/// <summary>
/// Publishes integration events to the broker. The concrete RabbitMQ
/// implementation is provided in a later phase; this phase defines the port only.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event wrapped in its metadata envelope.
    /// </summary>
    Task PublishAsync<TEvent>(EventEnvelope envelope, TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}
