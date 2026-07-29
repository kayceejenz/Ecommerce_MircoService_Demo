using InventoryService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace InventoryService.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(InventoryDbContext db, ILogger<OrderCancelledConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Processing OrderCancelled for OrderId: {OrderId}, Reason: {Reason}",
            message.OrderId, message.Reason);

        if (message.Items is null || message.Items.Count == 0)
        {
            _logger.LogInformation(
                "No items to release for Order {OrderId} — nothing was reserved.",
                message.OrderId);
            return;
        }

        var productIds = message.Items.Select(i => i.ProductId).ToList();
        var inventoryItems = await _db.InventoryItems
            .Where(i => productIds.Contains(i.Id))
            .ToListAsync();

        foreach (var item in message.Items)
        {
            var inventoryItem = inventoryItems.FirstOrDefault(i => i.Id == item.ProductId);
            if (inventoryItem is null)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found in inventory for release on Order {OrderId}.",
                    item.ProductId, message.OrderId);
                continue;
            }

            var released = Math.Min(item.Quantity, inventoryItem.ReservedQuantity);
            inventoryItem.AvailableQuantity += released;
            inventoryItem.ReservedQuantity -= released;
            inventoryItem.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Released {Released} of Product {ProductId} for Order {OrderId}. " +
                "Available: {Available}, Reserved: {Reserved}",
                released, item.ProductId, message.OrderId,
                inventoryItem.AvailableQuantity, inventoryItem.ReservedQuantity);
        }

        await _db.SaveChangesAsync();
    }
}
