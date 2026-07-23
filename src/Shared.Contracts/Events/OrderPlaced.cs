namespace Shared.Contracts.Events;

/// <summary>
/// Published when a new order is placed. Contains all order details
/// needed by consumers to perform their work.
/// </summary>
public record OrderPlaced
{
    /// <summary>Unique order identifier (GUID)</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who placed the order</summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Line items in the order. Each contains:
    ///   - ProductId: Which product
    ///   - Quantity: How many
    ///   - UnitPrice: Price at time of order (snapshot, not reference)
    /// </summary>
    public List<OrderItem> Items { get; init; } = new();

    /// <summary>Total order amount (sum of all line items)</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>ISO 8601 timestamp when the order was placed</summary>
    public DateTime PlacedAt { get; init; }
}

/// <summary>
/// Represents a single line item within an order.
/// </summary>
public record OrderItem
{
    /// <summary>Product identifier</summary>
    public Guid ProductId { get; init; }

    /// <summary>Quantity ordered</summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price at time of order.
    /// This is a SNAPSHOT - product prices may change later,
    /// but the order preserves the price the customer saw.
    /// </summary>
    public decimal UnitPrice { get; init; }
}
