using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(Sku.MaxLength).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(Product.MaxNameLength).IsRequired();
        builder.Property(p => p.MinimumStock).HasColumnName("minimum_stock");
        builder.Property(p => p.QuantityOnHand).HasColumnName("quantity_on_hand");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        // Optimistic concurrency: EF Core adds "WHERE version = <value read>" to every UPDATE of a product.
        // Zero rows affected means someone else saved first, and SaveChanges throws DbUpdateConcurrencyException.
        builder.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.CreatedAt);
        builder.Ignore(p => p.IsBelowMinimum);
        builder.Ignore(p => p.NewMovements);
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.ProductId).HasColumnName("product_id");
        builder.Property(m => m.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Change).HasColumnName("change");
        builder.Property(m => m.BalanceAfter).HasColumnName("balance_after");
        builder.Property(m => m.Reason).HasColumnName("reason").HasMaxLength(StockMovement.MaxReasonLength).IsRequired();
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at");

        builder.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.ProductId, m.OccurredAt });
    }
}
