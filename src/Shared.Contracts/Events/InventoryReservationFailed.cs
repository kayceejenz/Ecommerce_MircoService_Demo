namespace Shared.Contracts.Events;

/// <summary>
/// Published when inventory reservation fails (insufficient stock).
/// Triggers saga compensation - the order is cancelled.
/// </summary>
public record InventoryReservationFailed
{
    /// <summary>The order that couldn't be fulfilled</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who placed the order</summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Why the reservation failed.
    /// Example: "Insufficient stock for product X: requested 10, available 3"
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>ISO 8601 timestamp of the failure</summary>
    public DateTime FailedAt { get; init; }
}
