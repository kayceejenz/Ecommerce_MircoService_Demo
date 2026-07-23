namespace Shared.Contracts.Dtos;

/// <summary>
/// Notification sent to a customer.
/// </summary>
public record NotificationDto
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string Type { get; init; } = string.Empty;  // "OrderConfirmed", "PaymentFailed", etc.
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}
