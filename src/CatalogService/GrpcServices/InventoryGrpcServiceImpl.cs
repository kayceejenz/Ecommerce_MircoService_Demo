using Grpc.Core;
using CatalogService.Protos;
using System.Collections.Concurrent;

namespace CatalogService.GrpcServices;

/// <summary>
/// gRPC server implementation for real-time inventory streaming.
/// </summary>
public class InventoryGrpcServiceImpl(ILogger<InventoryGrpcServiceImpl> logger) : InventoryGrpcService.InventoryGrpcServiceBase
{
    private readonly ILogger<InventoryGrpcServiceImpl> _logger = logger;

    // Track all active streaming connections
    // Key: stream ID, Value: response stream writer
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<StockUpdate>> _clients = new();

    /// <summary>
    /// Server streaming method: subscribes client to real-time stock updates.
    ///
    /// FLOW:
    ///   1. Client sends StockWatchRequest (which products to monitor)
    ///   2. Server adds client to active subscribers
    ///   3. Server streams StockUpdate messages as stock changes
    ///   4. Stream continues until client disconnects or server shutdown
    /// </summary>
    public override async Task WatchStockLevels(
        StockWatchRequest request,
        IServerStreamWriter<StockUpdate> responseStream,
        ServerCallContext context)
    {
        var streamId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "gRPC client subscribed to stock updates. StreamId: {StreamId}, Products: {ProductCount}",
            streamId,
            request.ProductIds.Count);

        // Register this client for streaming updates
        _clients.TryAdd(streamId, responseStream);

        try
        {
            // Simulate initial stock data
            // In production, this would query the inventory database
            var productsToWatch = request.ProductIds.Count > 0
                ? request.ProductIds.ToList()
                : ["product-1", "product-2", "product-3"];

            // Send initial stock snapshot
            foreach (var productId in productsToWatch)
            {
                await responseStream.WriteAsync(new StockUpdate
                {
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    AvailableQuantity = Random.Shared.Next(5, 100),
                    ReservedQuantity = Random.Shared.Next(0, 10),
                    Reason = StockChangeReason.Restock
                });
            }

            // Keep connection open and simulate periodic updates
            // In production, this would be triggered by events from InventoryService
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);

                // Simulate a stock change
                var randomProduct = productsToWatch[Random.Shared.Next(productsToWatch.Count)];
                var change = Random.Shared.Next(-5, 10);

                await responseStream.WriteAsync(new StockUpdate
                {
                    ProductId = randomProduct,
                    ProductName = $"Product {randomProduct}",
                    AvailableQuantity = Math.Max(0, Random.Shared.Next(5, 100) + change),
                    ReservedQuantity = Random.Shared.Next(0, 10),
                    Reason = change > 0 ? StockChangeReason.Restock : StockChangeReason.OrderPlaced
                });

                _logger.LogDebug(
                    "Sent stock update for product {ProductId}, change: {Change}",
                    randomProduct,
                    change);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            _logger.LogInformation("gRPC client disconnected. StreamId: {StreamId}", streamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC stream. StreamId: {StreamId}", streamId);
        }
        finally
        {
            // Clean up when client disconnects
            _clients.TryRemove(streamId, out _);
            _logger.LogInformation("gRPC stream ended. StreamId: {StreamId}", streamId);
        }
    }

    /// <summary>
    /// Static method to push stock updates to ALL connected clients.
    /// Called by event handlers when stock changes (e.g., from InventoryService events).
    ///
    /// In production, this would be called by a MassTransit consumer that
    /// receives InventoryReserved/OrderCancelled events and broadcasts
    /// the changes to all subscribed CatalogService clients.
    /// </summary>
    public static async Task BroadcastStockUpdate(StockUpdate update)
    {
        var deadClients = new List<string>();

        foreach (var (streamId, stream) in _clients)
        {
            try
            {
                await stream.WriteAsync(update);
            }
            catch
            {
                // Client disconnected or error - mark for cleanup
                deadClients.Add(streamId);
            }
        }

        // Remove dead connections
        foreach (var id in deadClients)
        {
            _clients.TryRemove(id, out _);
        }
    }
}
