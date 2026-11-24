using System.Net.Http.Json;

namespace Inventory.IntegrationTests;

public class SampleDataTests(SeededInventoryApiFactory factory) : IClassFixture<SeededInventoryApiFactory>
{
    [Fact]
    public async Task Every_seeded_balance_equals_the_sum_of_its_history()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var products = await client.GetFromJsonAsync<PagedResponse<ProductView>>("/api/products?pageSize=100", ct);

        Assert.NotEmpty(products!.Items);
        Assert.Contains(products.Items, p => p.IsBelowMinimum);
        foreach (var product in products.Items)
        {
            var history = await client.GetFromJsonAsync<PagedResponse<MovementView>>($"/api/products/{product.Id}/movements?pageSize=100", ct);
            Assert.Equal(product.QuantityOnHand, history!.Items.Sum(m => m.Change));
        }
    }
}
