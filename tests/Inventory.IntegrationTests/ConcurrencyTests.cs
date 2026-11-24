using System.Net;
using System.Net.Http.Json;
using Inventory.Application.Abstractions;
using Inventory.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests;

/// <summary>Spec 003: two writers that read the same version cannot both succeed.</summary>
public class ConcurrencyTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    [Fact]
    public async Task Database_rejects_the_second_writer_of_the_same_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await CreateProductWithStockAsync(10, ct);

        // Two independent scopes = two users. Both load version 2 before either saves.
        await using var first = factory.Services.CreateAsyncScope();
        await using var second = factory.Services.CreateAsyncScope();
        var firstCopy = await first.ServiceProvider.GetRequiredService<IProductRepository>().GetByIdAsync(productId, ct);
        var secondCopy = await second.ServiceProvider.GetRequiredService<IProductRepository>().GetByIdAsync(productId, ct);

        firstCopy!.Issue(6, "User A", DateTimeOffset.UtcNow);
        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(ct);

        secondCopy!.Issue(6, "User B", DateTimeOffset.UtcNow); // passes in memory: this copy still sees 10 units
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(ct));

        var product = await factory.CreateClient().GetFromJsonAsync<ProductView>($"/api/products/{productId}", ct);
        Assert.Equal(4, product!.QuantityOnHand);
        Assert.Equal(3, product.Version);
    }

    [Fact]
    public async Task Simultaneous_requests_with_the_same_version_let_exactly_one_win()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await CreateProductWithStockAsync(10, ct);
        var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(1, 8).Select(i => client.PostAsJsonAsync(
            $"/api/products/{productId}/movements",
            new { type = "Issue", quantity = 1, reason = $"Request {i}", expectedVersion = 2 },
            ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var history = await client.GetFromJsonAsync<PagedResponse<MovementView>>($"/api/products/{productId}/movements", ct);
        Assert.Equal(2, history!.TotalCount);
        Assert.Equal(9, history.Items.Sum(m => m.Change));
    }

    private async Task<Guid> CreateProductWithStockAsync(int units, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var product = Product.Register(Sku.Create($"C-{Guid.NewGuid():N}"[..12]), "Concurrency test", 0, DateTimeOffset.UtcNow);
        product.Receive(units, "Initial stock", DateTimeOffset.UtcNow);
        scope.ServiceProvider.GetRequiredService<IProductRepository>().Add(product);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(ct);
        return product.Id;
    }
}
