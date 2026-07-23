namespace Shared.Contracts.Events;

/// <summary>
/// Published when an order is cancelled. Triggers compensation actions
/// in downstream services (release inventory, refund payment, notify customer).
/// </summary>
public record OrderCancelled
{
    /// <summary>The order that was cancelled</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who placed the order (for notification)</summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Why the order was cancelled.
    /// Examples: "Customer request", "Payment failed", "Inventory unavailable"
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>ISO 8601 timestamp when the cancellation occurred</summary>
    public DateTime CancelledAt { get; init; }
}
