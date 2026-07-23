namespace Shared.Contracts.Dtos;

/// <summary>
/// Request payload for creating a new order.
/// Sent by ApiGateway to OrderService.
/// </summary>
public record CreateOrderRequest
{
    /// <summary>Customer placing the order</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Items to order</summary>
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

/// <summary>
/// A single item in the order creation request.
/// </summary>
public record CreateOrderItemRequest
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
