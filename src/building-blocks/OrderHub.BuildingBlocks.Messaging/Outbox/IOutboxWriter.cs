namespace OrderHub.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Appends a message to the outbox within the current aggregate transaction.
/// </summary>
public interface IOutboxWriter
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
}
