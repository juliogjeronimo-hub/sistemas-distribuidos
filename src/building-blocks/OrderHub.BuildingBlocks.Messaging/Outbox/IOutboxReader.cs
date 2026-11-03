namespace OrderHub.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Drains pending messages for the publisher background service and marks them
/// published after broker confirmation.
/// </summary>
public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid messageId, DateTimeOffset processedAt, CancellationToken cancellationToken);
}
