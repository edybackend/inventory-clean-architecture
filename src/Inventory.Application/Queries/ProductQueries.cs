using Inventory.Domain;

namespace Inventory.Application.Queries;

/// <summary>Read side: returns DTOs straight from the database, without loading aggregates.</summary>
public interface IProductQueries
{
    Task<PagedResult<ProductSummary>> ListAsync(ProductFilter filter, CancellationToken cancellationToken);

    Task<ProductDetails?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<PagedResult<MovementView>?> ListMovementsAsync(Guid productId, PageRequest page, CancellationToken cancellationToken);
}

public sealed record PageRequest
{
    public const int MaxPageSize = 100;

    public PageRequest(int? page, int? pageSize)
    {
        Page = Math.Max(page ?? 1, 1);
        PageSize = Math.Clamp(pageSize ?? 20, 1, MaxPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}

public sealed record ProductFilter(string? Search, bool BelowMinimumOnly, PageRequest Page);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ProductSummary(Guid Id, string Sku, string Name, int QuantityOnHand, int MinimumStock, bool IsBelowMinimum);

public sealed record ProductDetails(
    Guid Id,
    string Sku,
    string Name,
    int QuantityOnHand,
    int MinimumStock,
    bool IsBelowMinimum,
    int Version,
    DateTimeOffset CreatedAt);

public sealed record MovementView(Guid Id, MovementType Type, int Change, int BalanceAfter, string Reason, DateTimeOffset OccurredAt);
