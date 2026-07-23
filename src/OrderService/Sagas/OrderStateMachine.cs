// WHAT: A state machine that orchestrates the distributed order workflow.
//
// SAGA PATTERN:
//   A saga coordinates multiple services to complete a business transaction.
//   It manages the state machine and handles compensating actions on failure.
//
// WHY SAGA:
//   - Distributed transactions (2PC) are complex and slow
//   - Saga breaks the transaction into local steps with compensation
//   - Each step has a forward action and a compensating action
//   - If any step fails, compensating actions undo previous steps
//
// STATE MACHINE:
//
//   ┌─────────┐
//   │ Started │
//   └────┬────┘
//        │
//        ▼
//   ┌──────────────────┐     ┌─────────────────────┐
//   │ InventoryReserved │────►│  PaymentProcessing  │
//   └──────────────────┘     └─────────┬───────────┘
//                                      │
//                              ┌───────┴───────┐
//                              │               │
//                              ▼               ▼
//                     ┌─────────────┐  ┌─────────────┐
//                     │  Completed  │  │   Failed    │
//                     └─────────────┘  └──────┬──────┘
//                                             │
//                                             ▼
//                                      ┌─────────────┐
//                                      │ Compensating│
//                                      └─────────────┘
//
// MASS TRANSIT:
//   MassTransit provides a powerful state machine framework.
//   - States: Define the possible states of the saga
//   - Events: Trigger state transitions
//   - Activities: Execute business logic during transitions
//   - Persistence: Saga state is stored in PostgreSQL (survives restarts)
//
// MESSAGE FLOW:
//   1. OrderPlaced event -> StateMachine starts -> sends ReserveInventory command
//   2. InventoryReserved event -> StateMachine transitions -> sends ProcessPayment command
//   3. PaymentSucceeded event -> StateMachine transitions -> Order Completed
//   4. PaymentFailed event -> StateMachine transitions -> sends ReleaseInventory command

using MassTransit;
using Shared.Contracts.Events;

namespace OrderService.Sagas;

// This entity stores the current state of each saga instance.
// MassTransit automatically manages this via Entity Framework Core.
//
// WHY PERSIST SAGA STATE:
//   - Sagas can span minutes/hours (wait for payment, shipping)
//   - Service might restart during the saga
//   - State must survive restarts to resume correctly
// =============================================================================
public class OrderSagaState : SagaStateMachineInstance
{
    /// <summary>Unique saga instance ID (typically the OrderId)</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Current state of the saga (MassTransit tracks this)</summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>Order being processed</summary>
    public Guid OrderId { get; set; }

    /// <summary>Customer who placed the order</summary>
    public Guid CustomerId { get; set; }

    /// <summary>When the saga started</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>When the saga completed (success or failure)</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Failure reason if saga failed</summary>
    public string? FailureReason { get; set; }
}

// MassTransit uses a fluent API to define:
//   - States: Possible saga states
//   - Events: Messages that trigger transitions
//   - Transitions: State -> Event -> New State
//   - Activities: Business logic executed during transitions
public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    // Each state represents a stage in the order processing workflow.
    // The saga "sits" in a state, waiting for the next event.
    public State Started { get; private set; } = null!;
    public State InventoryReserved { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Events trigger state transitions.
    // Each event is a message published on RabbitMQ.
    public Event<OrderPlaced> OrderPlaced { get; private set; } = null!;
    public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = null!;
    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; } = null!;
    public Event<PaymentSucceeded> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;
    public OrderStateMachine()
    {
        // Use PostgreSQL for saga state persistence
        // State is stored in the "order_saga_state" table
        InstanceState(x => x.CurrentState);

        // When an order is placed:
        //   1. Create saga instance (store in PostgreSQL)
        //   2. Transition to "Started" state
        //   3. Send "ReserveInventory" command to InventoryService
        //
        // The command is sent via RabbitMQ to the InventoryService queue.
        // InventoryService will process it and publish InventoryReserved or
        // InventoryReservationFailed event.
        Event(() => OrderPlaced, x => x
            .CorrelateById(context => context.Message.OrderId)
            .SelectId(context => context.Message.OrderId));

        Initially(
            When(OrderPlaced)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.StartedAt = DateTime.UtcNow;

                    Log(context, "Saga started for order {OrderId}", context.Message.OrderId);
                })
                .TransitionTo(Started)
                .Publish(context => new ReserveInventoryCommand
                {
                    OrderId = context.Message.OrderId,
                    CustomerId = context.Message.CustomerId,
                    Items = context.Message.Items.Select(i => new ReserveInventoryItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList()
                }));

        // When inventory is reserved:
        //   1. Transition to "InventoryReserved" state
        //   2. Send "ProcessPayment" command to PaymentService
        During(Started,
            When(InventoryReservedEvent)
                .Then(context =>
                {
                    context.Saga.CompletedAt = null;
                    context.Saga.FailureReason = null;

                    Log(context, "Inventory reserved for order {OrderId}", context.Message.OrderId);
                })
                .TransitionTo(InventoryReserved)
                .Publish(context => new ProcessPaymentCommand
                {
                    OrderId = context.Message.OrderId,
                    CustomerId = context.Message.CustomerId,
                    Amount = 0  // Would be calculated from order items
                }));

        // If inventory can't be reserved:
        //   1. Transition to "Failed" state
        //   2. Publish OrderCancelled event (compensating action)
        //   3. No payment needed, saga ends
        During(Started,
            When(InventoryReservationFailed)
                .Then(context =>
                {
                    context.Saga.CompletedAt = DateTime.UtcNow;
                    context.Saga.FailureReason = context.Message.Reason;

                    Log(context, "Inventory reservation failed for order {OrderId}: {Reason}",
                        context.Message.OrderId, context.Message.Reason);
                })
                .TransitionTo(Failed)
                .Publish(context => new OrderCancelled
                {
                    OrderId = context.Message.OrderId,
                    CustomerId = context.Message.CustomerId,
                    Reason = context.Message.Reason,
                    CancelledAt = DateTime.UtcNow
                }));

        // When payment succeeds:
        //   1. Transition to "Completed" state
        //   2. Publish OrderConfirmed event (optional)
        //   3. Saga is complete, state is persisted
        During(InventoryReserved,
            When(PaymentSucceeded)
                .Then(context =>
                {
                    context.Saga.CompletedAt = DateTime.UtcNow;

                    Log(context, "Payment succeeded for order {OrderId}. Saga complete!",
                        context.Message.OrderId);
                })
                .TransitionTo(Completed));

        // When payment fails:
        //   1. Transition to "Failed" state
        //   2. Send "ReleaseInventory" command to InventoryService
        //      (compensating action to release reserved stock)
        //   3. Publish OrderCancelled event
        //   4. Saga ends
        During(InventoryReserved,
            When(PaymentFailed)
                .Then(context =>
                {
                    context.Saga.CompletedAt = DateTime.UtcNow;
                    context.Saga.FailureReason = context.Message.Reason;

                    Log(context, "Payment failed for order {OrderId}: {Reason}. Releasing inventory.",
                        context.Message.OrderId, context.Message.Reason);
                })
                .TransitionTo(Failed)
                .Publish(context => new ReleaseInventoryCommand
                {
                    OrderId = context.Message.OrderId
                })
                .Publish(context => new OrderCancelled
                {
                    OrderId = context.Message.OrderId,
                    CustomerId = context.Message.CustomerId,
                    Reason = $"Payment failed: {context.Message.Reason}",
                    CancelledAt = DateTime.UtcNow
                }));
    }

    private static void Log<T>(BehaviorContext<OrderSagaState, T> context, string message, params object[] args) where T : class
    {
        // Use MassTransit's built-in logging via the context
        var loggerFactory = context.GetPayload<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<OrderStateMachine>();
        logger.LogInformation(message, args);
    }
}

public record ReserveInventoryCommand
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public List<ReserveInventoryItem> Items { get; init; } = new();
}

public record ReserveInventoryItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record ReleaseInventoryCommand
{
    public Guid OrderId { get; init; }
}

public record ProcessPaymentCommand
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
}
