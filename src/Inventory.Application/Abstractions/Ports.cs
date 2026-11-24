using Inventory.Domain;

namespace Inventory.Application.Abstractions;

/// <summary>Write-side persistence for the Product aggregate (implemented with EF Core).</summary>
public interface IProductRepository
{
    void Add(Product product);

    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken);
}

/// <summary>Commits every change tracked in the current scope atomically.</summary>
public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyConflictException">Another request changed the same product first.</exception>
    /// <exception cref="DuplicateKeyException">A unique key, such as the SKU, already exists.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The row was changed by someone else between read and write (optimistic concurrency).</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The record was changed by another request.", innerException);

/// <summary>A unique constraint was violated when saving.</summary>
public sealed class DuplicateKeyException(Exception innerException)
    : Exception("A record with the same unique key already exists.", innerException);
