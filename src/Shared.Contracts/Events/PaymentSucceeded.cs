namespace Shared.Contracts.Events;

/// <summary>
/// Published when a payment is successfully processed.
/// Triggers order confirmation and customer notification.
/// </summary>
public record PaymentSucceeded
{
    /// <summary>The order this payment was for</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who made the payment</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Amount successfully charged</summary>
    public decimal Amount { get; init; }

    /// <summary>Payment provider transaction ID (for auditing)</summary>
    public string TransactionId { get; init; } = string.Empty;

    /// <summary>ISO 8601 timestamp when payment was processed</summary>
    public DateTime ProcessedAt { get; init; }
}
