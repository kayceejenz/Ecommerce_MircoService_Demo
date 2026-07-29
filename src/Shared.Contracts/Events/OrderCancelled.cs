namespace Shared.Contracts.Events;

public record OrderCancelled
{
    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTime CancelledAt { get; init; }

    public List<OrderItem>? Items { get; init; }
}
