using Inventory.Application.Queries;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Queries;

/// <summary>Read side: no-tracking LINQ projections straight to DTOs. Aggregates are never loaded to answer a query.</summary>
internal sealed class EfProductQueries(InventoryDbContext db) : IProductQueries
{
    public async Task<PagedResult<ProductSummary>> ListAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{EscapeLike(filter.Search.Trim())}%";
            query = query.Where(p => EF.Functions.Like(p.Sku, pattern, "\\") || EF.Functions.Like(p.Name, pattern, "\\"));
        }

        if (filter.BelowMinimumOnly)
        {
            query = query.Where(p => p.QuantityOnHand < p.MinimumStock);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Sku)
            .Skip(filter.Page.Skip)
            .Take(filter.Page.PageSize)
            .Select(p => new ProductSummary(p.Id, p.Sku, p.Name, p.QuantityOnHand, p.MinimumStock, p.QuantityOnHand < p.MinimumStock))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummary>(items, filter.Page.Page, filter.Page.PageSize, total);
    }

    public Task<ProductDetails?> GetByIdAsync(Guid productId, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetails(
                p.Id, p.Sku, p.Name, p.QuantityOnHand, p.MinimumStock, p.QuantityOnHand < p.MinimumStock, p.Version, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<MovementView>?> ListMovementsAsync(Guid productId, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!await db.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            return null;
        }

        var query = db.Movements.AsNoTracking().Where(m => m.ProductId == productId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(m => new MovementView(m.Id, m.Type, m.Change, m.BalanceAfter, m.Reason, m.OccurredAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<MovementView>(items, page.Page, page.PageSize, total);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
