namespace Shared.Contracts.Events;

/// <summary>
/// Published when a payment fails. Triggers saga compensation
/// and customer notification about the failure.
/// </summary>
public record PaymentFailed
{
    /// <summary>The order whose payment failed</summary>
    public Guid OrderId { get; init; }

    /// <summary>Customer who attempted the payment</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Amount that failed to process</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Why the payment failed.
    /// Examples: "Insufficient funds", "Card declined", "Timeout"
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>ISO 8601 timestamp of the failure</summary>
    public DateTime FailedAt { get; init; }
}
