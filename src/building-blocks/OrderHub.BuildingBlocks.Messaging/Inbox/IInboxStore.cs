namespace OrderHub.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Guards consumers against duplicate delivery. Implementations persist the
/// (<c>MessageId</c>, <c>Consumer</c>) pair in the same transaction as the side
/// effect, so a redelivered message is detected and skipped.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Returns <c>true</c> when the given message has already been processed by the consumer.
    /// </summary>
    Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken);

    /// <summary>
    /// Records that the consumer processed the message, within the current transaction.
    /// </summary>
    Task MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken);
}
