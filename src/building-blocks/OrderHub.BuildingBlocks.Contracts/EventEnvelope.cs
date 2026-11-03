namespace OrderHub.BuildingBlocks.Contracts;

/// <summary>
/// Metadata envelope that wraps every integration event exchanged between services.
/// </summary>
public sealed record EventEnvelope
{
    public required Guid MessageId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required string CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public required string Type { get; init; }

    public required int Version { get; init; }

    public required string Producer { get; init; }
}
