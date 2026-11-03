namespace OrderHub.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Persistence-agnostic description of a message stored in the transactional outbox
/// (<c>outbox_messages</c>), written in the same transaction as the aggregate change.
/// </summary>
public sealed record OutboxMessage
{
    public required Guid MessageId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required string CorrelationId { get; init; }

    public string? CausationId { get; init; }

    /// <summary>Logical event name used to build the routing key.</summary>
    public required string Type { get; init; }

    public required int Version { get; init; }

    public required string Producer { get; init; }

    /// <summary>Serialized event payload.</summary>
    public required string Payload { get; init; }

    /// <summary>Instant the message was confirmed as published, or <c>null</c> while pending.</summary>
    public DateTimeOffset? ProcessedAt { get; init; }
}
