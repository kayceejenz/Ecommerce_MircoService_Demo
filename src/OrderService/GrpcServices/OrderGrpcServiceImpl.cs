using Grpc.Core;
using MediatR;
using OrderService.Cqrs.Commands;
using OrderService.Cqrs.Queries;
using OrderService.Protos;

namespace OrderService.GrpcServices;

/// <summary>
/// gRPC server implementation for order operations.
/// </summary>
public class OrderGrpcServiceImpl : OrderGrpcService.OrderGrpcServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderGrpcServiceImpl> _logger;

    public OrderGrpcServiceImpl(IMediator mediator, ILogger<OrderGrpcServiceImpl> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create an order via gRPC.
    /// Converts gRPC request to MediatR command and processes it.
    /// </summary>
    public override async Task<CreateOrderGrpcResponse> CreateOrder(
        CreateOrderGrpcRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CreateOrder - Customer: {CustomerId}", request.CustomerId);

        var command = new PlaceOrderCommand
        {
            CustomerId = Guid.Parse(request.CustomerId),
            Items = request.Items.Select(i => new PlaceOrderItemCommand
            {
                ProductId = Guid.Parse(i.ProductId),
                Quantity = i.Quantity
            }).ToList()
        };

        var result = await _mediator.Send(command);

        return new CreateOrderGrpcResponse
        {
            OrderId = result.OrderId.ToString(),
            Success = result.Success,
            ErrorMessage = result.ErrorMessage ?? string.Empty
        };
    }

    /// <summary>
    /// Get an order via gRPC.
    /// Reads from the PostgreSQL read model.
    /// </summary>
    public override async Task<OrderGrpcResponse> GetOrder(
        GetOrderGrpcRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetOrder - OrderId: {OrderId}", request.OrderId);

        var query = new GetOrderQuery { OrderId = Guid.Parse(request.OrderId) };
        var order = await _mediator.Send(query);

        if (order is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));
        }

        return new OrderGrpcResponse
        {
            OrderId = order.OrderId.ToString(),
            CustomerId = order.CustomerId.ToString(),
            TotalAmount = (double)order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt.ToString("O")
        };
    }
}
