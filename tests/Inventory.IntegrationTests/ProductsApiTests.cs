using System.Net;
using System.Net.Http.Json;

namespace Inventory.IntegrationTests;

public class ProductsApiTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueSku() => $"T-{Guid.NewGuid():N}"[..12];

    private async Task<ProductView> RegisterAsync(string sku, int minimumStock = 5)
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.PostAsJsonAsync("/api/products", new { sku, name = "Test product", minimumStock }, ct);
        response.EnsureSuccessStatusCode();
        return (await _client.GetFromJsonAsync<ProductView>(response.Headers.Location, ct))!;
    }

    private Task<HttpResponseMessage> MoveAsync(Guid id, string type, int quantity, int expectedVersion, string reason = "Test") =>
        _client.PostAsJsonAsync(
            $"/api/products/{id}/movements",
            new { type, quantity, reason, expectedVersion },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Post_registers_a_product_readable_through_the_location_header()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/api/products", new { sku = "ab-123", name = " Keyboard ", minimumStock = 2 }, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedProduct>(ct);
        var product = await _client.GetFromJsonAsync<ProductView>(response.Headers.Location, ct);
        Assert.Equal(created!.Id, product!.Id);
        Assert.Equal("AB-123", product.Sku);
        Assert.Equal("Keyboard", product.Name);
        Assert.Equal(0, product.QuantityOnHand);
        Assert.Equal(1, product.Version);
    }

    [Fact]
    public async Task Post_with_invalid_fields_returns_a_validation_problem_for_each_field()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/api/products", new { sku = "x", name = "", minimumStock = -1 }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Problem>(ct);
        Assert.Equal(["minimumStock", "name", "sku"], problem!.Errors!.Keys.Order());
    }

    [Fact]
    public async Task Post_with_existing_sku_in_another_case_returns_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var sku = UniqueSku();
        await RegisterAsync(sku);

        var response = await _client.PostAsJsonAsync("/api/products", new { sku = sku.ToLowerInvariant(), name = "Copy", minimumStock = 0 }, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Movements_change_the_balance_and_the_history_explains_it()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await RegisterAsync(UniqueSku(), minimumStock: 5);

        (await MoveAsync(product.Id, "Receipt", 10, expectedVersion: 1)).EnsureSuccessStatusCode();
        (await MoveAsync(product.Id, "Issue", 4, expectedVersion: 2)).EnsureSuccessStatusCode();
        var last = await MoveAsync(product.Id, "Adjustment", 3, expectedVersion: 3, reason: "Cycle count");

        var result = await last.Content.ReadFromJsonAsync<MovementResult>(ct);
        Assert.Equal(3, result!.QuantityOnHand);
        Assert.Equal(4, result.Version);
        Assert.True(result.IsBelowMinimum);

        var history = await _client.GetFromJsonAsync<PagedResponse<MovementView>>($"/api/products/{product.Id}/movements", ct);
        Assert.Equal(["Adjustment", "Issue", "Receipt"], history!.Items.Select(m => m.Type));
        Assert.Equal(result.QuantityOnHand, history.Items.Sum(m => m.Change));
        Assert.Equal(3, history.Items[0].BalanceAfter);
    }

    [Fact]
    public async Task Issue_above_the_balance_returns_422_and_changes_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await RegisterAsync(UniqueSku());
        await MoveAsync(product.Id, "Receipt", 2, expectedVersion: 1);

        var response = await MoveAsync(product.Id, "Issue", 3, expectedVersion: 2);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Problem>(ct);
        Assert.Equal(2, problem!.QuantityOnHand);
        var current = await _client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}", ct);
        Assert.Equal(2, current!.QuantityOnHand);
        Assert.Equal(2, current.Version);
    }

    [Fact]
    public async Task Stale_expected_version_returns_409_with_the_current_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await RegisterAsync(UniqueSku());
        await MoveAsync(product.Id, "Receipt", 5, expectedVersion: 1);

        var response = await MoveAsync(product.Id, "Issue", 1, expectedVersion: 1);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Problem>(ct);
        Assert.Equal(2, problem!.CurrentVersion);
    }

    [Fact]
    public async Task Unknown_movement_type_is_a_bad_request()
    {
        var product = await RegisterAsync(UniqueSku());

        var response = await MoveAsync(product.Id, "Steal", 1, expectedVersion: 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/products/{Guid.NewGuid()}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/products/{Guid.NewGuid()}/movements", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await MoveAsync(Guid.NewGuid(), "Receipt", 1, 1)).StatusCode);
    }

    [Fact]
    public async Task List_filters_by_search_and_below_minimum_and_limits_the_page_size()
    {
        var ct = TestContext.Current.CancellationToken;
        var marker = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var low = await RegisterAsync($"LOW-{marker}", minimumStock: 5);
        var ok = await RegisterAsync($"OK-{marker}", minimumStock: 0);

        var search = await _client.GetFromJsonAsync<PagedResponse<ProductView>>($"/api/products?search={marker.ToLowerInvariant()}", ct);
        var belowMinimum = await _client.GetFromJsonAsync<PagedResponse<ProductView>>($"/api/products?search={marker}&belowMinimum=true", ct);
        var hugePage = await _client.GetFromJsonAsync<PagedResponse<ProductView>>("/api/products?pageSize=1000", ct);

        Assert.Equal(2, search!.TotalCount);
        Assert.Equal(low.Id, Assert.Single(belowMinimum!.Items).Id);
        Assert.DoesNotContain(belowMinimum.Items, p => p.Id == ok.Id);
        Assert.Equal(100, hugePage!.PageSize);
    }

    [Fact]
    public async Task Like_wildcards_in_the_search_are_treated_as_text()
    {
        var ct = TestContext.Current.CancellationToken;
        await RegisterAsync(UniqueSku());

        var result = await _client.GetFromJsonAsync<PagedResponse<ProductView>>("/api/products?search=%25", ct);

        Assert.Equal(0, result!.TotalCount);
    }

    [Fact]
    public async Task Unknown_api_route_is_404_not_the_spa_page()
    {
        var response = await _client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
