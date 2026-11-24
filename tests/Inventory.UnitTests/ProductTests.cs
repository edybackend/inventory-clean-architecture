using Inventory.Domain;

namespace Inventory.UnitTests;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Product NewProduct(int minimumStock = 5) => Product.Register(Sku.Create("kb-100"), "Mechanical keyboard", minimumStock, Now);

    [Fact]
    public void Register_starts_with_zero_units_and_version_1()
    {
        var product = NewProduct();

        Assert.Equal("KB-100", product.Sku.Value);
        Assert.Equal(0, product.QuantityOnHand);
        Assert.Equal(1, product.Version);
        Assert.Empty(product.NewMovements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Sku_rejects_invalid_values(string value) => Assert.Throws<DomainException>(() => Sku.Create(value));

    [Fact]
    public void Sku_is_compared_by_normalized_value() => Assert.Equal(Sku.Create(" kb-100 "), Sku.Create("KB-100"));

    [Theory]
    [InlineData("", 0)]
    [InlineData("Keyboard", -1)]
    public void Register_rejects_invalid_values(string name, int minimumStock) =>
        Assert.Throws<DomainException>(() => Product.Register(Sku.Create("KB-100"), name, minimumStock, Now));

    [Fact]
    public void Receipt_adds_units_and_records_the_movement()
    {
        var product = NewProduct();

        var movement = product.Receive(10, "Supplier invoice 123", Now);

        Assert.Equal(10, product.QuantityOnHand);
        Assert.Equal(2, product.Version);
        Assert.Equal(MovementType.Receipt, movement.Type);
        Assert.Equal(10, movement.Change);
        Assert.Equal(10, movement.BalanceAfter);
        Assert.Equal(product.Id, movement.ProductId);
    }

    [Fact]
    public void Withdrawal_above_balance_is_rejected_and_nothing_changes()
    {
        var product = NewProduct();
        product.Receive(3, "Initial stock", Now);

        var error = Assert.Throws<InsufficientStockException>(() => product.Issue(4, "Order 42", Now));

        Assert.Equal(4, error.Requested);
        Assert.Equal(3, error.OnHand);
        Assert.Equal(3, product.QuantityOnHand);
        Assert.Equal(2, product.Version);
        Assert.Single(product.NewMovements);
    }

    [Fact]
    public void Issue_of_the_whole_balance_is_allowed()
    {
        var product = NewProduct();
        product.Receive(3, "Initial stock", Now);

        var movement = product.Issue(3, "Order 42", Now);

        Assert.Equal(0, product.QuantityOnHand);
        Assert.Equal(-3, movement.Change);
    }

    [Fact]
    public void Adjustment_sets_the_counted_quantity_and_stores_the_difference()
    {
        var product = NewProduct();
        product.Receive(10, "Initial stock", Now);

        var movement = product.Adjust(7, "Cycle count", Now);

        Assert.Equal(7, product.QuantityOnHand);
        Assert.Equal(-3, movement.Change);
        Assert.Equal(MovementType.Adjustment, movement.Type);
    }

    [Fact]
    public void Adjustment_must_change_the_balance()
    {
        var product = NewProduct();
        product.Receive(10, "Initial stock", Now);

        Assert.Throws<DomainException>(() => product.Adjust(10, "Cycle count", Now));
    }

    [Theory]
    [InlineData(0, "Reason")]
    [InlineData(-1, "Reason")]
    [InlineData(1, " ")]
    public void Movements_require_positive_quantity_and_a_reason(int quantity, string reason)
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() => product.Receive(quantity, reason, Now));
    }

    [Fact]
    public void Balance_always_equals_the_sum_of_the_history()
    {
        var product = NewProduct();

        product.Receive(20, "Initial stock", Now);
        product.Issue(5, "Order 1", Now);
        product.Adjust(12, "Cycle count", Now);
        product.Issue(2, "Order 2", Now);
        product.Receive(4, "Return", Now);

        Assert.Equal(product.NewMovements.Sum(m => m.Change), product.QuantityOnHand);
        Assert.Equal(product.QuantityOnHand, product.NewMovements[^1].BalanceAfter);
        Assert.Equal(6, product.Version);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void Below_minimum_compares_balance_with_minimum_stock(int units, bool expected)
    {
        var product = NewProduct(minimumStock: 5);
        product.Receive(units, "Initial stock", Now);

        Assert.Equal(expected, product.IsBelowMinimum);
    }
}
