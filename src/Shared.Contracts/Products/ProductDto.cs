namespace Shared.Contracts.Dtos;

/// <summary>
/// Product data transfer object shared between services.
/// Used in REST API responses and inter-service communication.
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
}
