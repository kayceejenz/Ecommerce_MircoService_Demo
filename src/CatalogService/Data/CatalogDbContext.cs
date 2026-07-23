using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

/// <summary>
/// EF Core DbContext for the Catalog database.
/// Provides access to the Products table and handles change tracking.
/// </summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Description)
                .HasMaxLength(2000);
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0);
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("idx_products_category");
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("idx_products_name");
        });
    }
}
