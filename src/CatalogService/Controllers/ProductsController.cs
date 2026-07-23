using CatalogService.Data;
using CatalogService.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    CatalogDbContext db,
    IConnectionMultiplexer redis,
    ILogger<ProductsController> logger) : ControllerBase
{
    private readonly CatalogDbContext _db = db;
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly ILogger<ProductsController> _logger = logger;

    private const string CachePrefix = "catalog:product:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category = null)
    {
        _logger.LogInformation("GET /api/products - Category: {Category}", category ?? "all");

        // Build cache key based on query parameters
        var cacheKey = string.IsNullOrEmpty(category)
            ? $"{CachePrefix}all"
            : $"{CachePrefix}category:{category}";

        // Try Redis cache first
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            var products = JsonSerializer.Deserialize<List<Product>>((string)cached!);
            return Ok(products);
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}, querying database", cacheKey);

        // Cache miss - query PostgreSQL
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var result = await query.OrderBy(p => p.Name).ToListAsync();

        // Cache the result in Redis
        var serialized = JsonSerializer.Serialize(result);
        await db.StringSetAsync(cacheKey, serialized, CacheDuration);

        _logger.LogInformation("Returning {Count} products", result.Count);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("GET /api/products/{ProductId}", id);

        var cacheKey = $"{CachePrefix}{id}";
        var db = _redis.GetDatabase();

        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            _logger.LogDebug("Cache HIT for product {ProductId}", id);
            var product = JsonSerializer.Deserialize<Product>((string)cached!);
            return Ok(product);
        }

        _logger.LogDebug("Cache MISS for product {ProductId}, querying database", id);

        var entity = await _db.Products.FindAsync(id);
        if (entity is null)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return NotFound(new { error = $"Product {id} not found" });
        }

        // Cache individual product
        var serialized = JsonSerializer.Serialize(entity);
        await db.StringSetAsync(cacheKey, serialized, CacheDuration);

        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("POST /api/products - Name: {Name}", request.Name);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            StockQuantity = request.InitialStock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Invalidate list caches (new product affects list results)
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{CachePrefix}all");
        await db.KeyDeleteAsync($"{CachePrefix}category:{product.Category}");

        _logger.LogInformation("Created product {ProductId}", product.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }


    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        _logger.LogInformation("PUT /api/products/{ProductId}", id);

        var product = await _db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound(new { error = $"Product {id} not found" });
        }

        // Update only provided fields
        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Category is not null) product.Category = request.Category;
        if (request.StockQuantity.HasValue) product.StockQuantity = request.StockQuantity.Value;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Invalidate caches
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{CachePrefix}{id}");
        await db.KeyDeleteAsync($"{CachePrefix}all");
        await db.KeyDeleteAsync($"{CachePrefix}category:{product.Category}");

        _logger.LogInformation("Updated product {ProductId}", id);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("DELETE /api/products/{ProductId}", id);

        var product = await _db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound(new { error = $"Product {id} not found" });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        // Invalidate caches
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{CachePrefix}{id}");
        await db.KeyDeleteAsync($"{CachePrefix}all");
        await db.KeyDeleteAsync($"{CachePrefix}category:{product.Category}");

        _logger.LogInformation("Deleted product {ProductId}", id);
        return NoContent();
    }
}

#region dto
public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
    public int InitialStock { get; init; }
}

public record UpdateProductRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? Category { get; init; }
    public int? StockQuantity { get; init; }
}
#endregion