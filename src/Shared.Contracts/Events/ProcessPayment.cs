namespace Shared.Contracts.Events;

/// <summary>
/// Command sent by the OrderSaga to process a payment for an order.
/// PaymentService consumes this and publishes PaymentSucceeded or PaymentFailed.
/// </summary>
public record ProcessPaymentCommand
{
    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public decimal Amount { get; init; }
}
