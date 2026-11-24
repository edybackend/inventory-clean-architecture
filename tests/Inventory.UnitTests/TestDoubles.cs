using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.UnitTests;

internal sealed class InMemoryProducts : IProductRepository, IUnitOfWork
{
    private readonly Dictionary<Guid, Product> _products = [];

    public int SaveCount { get; private set; }

    /// <summary>When set, the next save throws it once (simulates the database refusing the write).</summary>
    public Exception? FailNextSaveWith { get; set; }

    public void Add(Product product) => _products[product.Id] = product;

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken) =>
        Task.FromResult(_products.GetValueOrDefault(productId));

    public Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken) =>
        Task.FromResult(_products.Values.Any(p => p.Sku == sku.Value));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSaveWith is { } failure)
        {
            FailNextSaveWith = null;
            throw failure;
        }

        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
