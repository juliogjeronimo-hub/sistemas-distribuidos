using System.Diagnostics.CodeAnalysis;

namespace OrderHub.Catalog.Domain.Common;

/// <summary>
/// Represents a business or domain error with a stable code and a human-readable message.
/// Codes follow the pattern "Entity.Reason" (e.g. "Product.NotFound").
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Error is the canonical name for this Result-pattern type; renaming would harm readability.")]
public sealed record Error(string Code, string Message)
{
    /// <summary>Sentinel used when no real error is present (should not reach callers).</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates an error for a resource that could not be found.</summary>
    public static Error NotFound(string entity, string id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    /// <summary>Creates a validation error with an explicit message.</summary>
    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);

    public override string ToString() => $"{Code}: {Message}";
}
