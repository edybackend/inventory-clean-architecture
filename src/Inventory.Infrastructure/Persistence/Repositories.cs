using Inventory.Application.Abstractions;
using Inventory.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class ProductRepository(InventoryDbContext db) : IProductRepository
{
    public void Add(Product product) => db.Products.Add(product);

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken) =>
        db.Products.AnyAsync(p => p.Sku == sku.Value, cancellationToken);
}

internal sealed class EfUnitOfWork(InventoryDbContext db) : IUnitOfWork
{
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintUnique = 2067;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // The history is never loaded to change stock, so new movements are added explicitly.
        foreach (var product in db.ChangeTracker.Entries<Product>().Select(entry => entry.Entity).ToList())
        {
            foreach (var movement in product.NewMovements)
            {
                if (db.Entry(movement).State == EntityState.Detached)
                {
                    db.Movements.Add(movement);
                }
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint, SqliteExtendedErrorCode: SqliteConstraintUnique })
        {
            throw new DuplicateKeyException(ex);
        }
    }
}
