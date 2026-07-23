namespace Shared.Contracts.Events;

/// <summary>
/// Published when inventory is successfully reserved for an order.
/// Signals the saga orchestrator to proceed to the payment step.
/// </summary>
public record InventoryReserved
{
    /// <summary>The order this reservation is for</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who placed the order</summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Items that were reserved. Each confirms the exact quantity
    /// that was decremented from available stock.
    /// </summary>
    public List<ReservedItem> ReservedItems { get; init; } = new();

    /// <summary>ISO 8601 timestamp of the reservation</summary>
    public DateTime ReservedAt { get; init; }
}

/// <summary>
/// A single reserved item within an inventory reservation.
/// </summary>
public record ReservedItem
{
    /// <summary>Product identifier</summary>
    public Guid ProductId { get; init; }

    /// <summary>Quantity successfully reserved</summary>
    public int Quantity { get; init; }
}
