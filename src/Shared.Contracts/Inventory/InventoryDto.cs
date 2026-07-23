// =============================================================================
// InventoryDto - Shared Inventory Data Transfer Object
// =============================================================================
// WHY: Used by CatalogService and InventoryService to represent stock levels.
// =============================================================================

namespace Shared.Contracts.Dtos;

/// <summary>
/// Inventory/stock data for a product.
/// </summary>
public record InventoryDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int ReorderThreshold { get; init; }
    public bool IsLowStock => AvailableQuantity < ReorderThreshold;
}
