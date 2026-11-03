namespace OrderHub.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Persistence-agnostic description of a message recorded in the idempotency inbox
/// (<c>inbox_messages</c>). A message is uniquely identified by its
/// <see cref="MessageId"/> together with the <see cref="Consumer"/> that handled it,
/// enabling at-least-once delivery to be processed exactly once per consumer.
/// </summary>
public sealed record InboxMessage
{
    public required Guid MessageId { get; init; }

    /// <summary>Logical name of the consumer that processed the message.</summary>
    public required string Consumer { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }
}
