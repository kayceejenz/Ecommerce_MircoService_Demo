namespace Shared.Contracts.Events;

/// <summary>
/// Published when inventory for a product falls below the reorder threshold.
/// Triggers restocking alerts and notifications.
/// </summary>
public record InventoryLow
{
    /// <summary>Product with low stock</summary>
    public Guid ProductId { get; init; }

    /// <summary>Product name (denormalized for notification display)</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Current stock level (below threshold)</summary>
    public int CurrentQuantity { get; init; }

    /// <summary>
    /// The threshold that was breached.
    /// Example: if threshold is 10 and current is 3, this means
    /// "we normally reorder when stock drops below 10"
    /// </summary>
    public int ReorderThreshold { get; init; }

    /// <summary>ISO 8601 timestamp when low stock was detected</summary>
    public DateTime DetectedAt { get; init; }
}
