using Inventory.Application;
using Inventory.Application.Abstractions;
using Inventory.Application.Movements;
using Inventory.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inventory.UnitTests;

public class RecordMovementHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly InMemoryProducts _store = new();
    private readonly Product _product;

    public RecordMovementHandlerTests()
    {
        _product = Product.Register(Sku.Create("KB-100"), "Keyboard", 5, Now);
        _product.Receive(10, "Initial stock", Now); // version 2
        _store.Add(_product);
    }

    private RecordMovementHandler CreateHandler() =>
        new(_store, _store, new RecordMovementValidator(), new FixedClock(Now), NullLogger<RecordMovementHandler>.Instance);

    private Task<Result<MovementRecorded>> Handle(MovementType type, int quantity, int expectedVersion = 2, string reason = "Order 42") =>
        CreateHandler().HandleAsync(new RecordMovementCommand(_product.Id, type, quantity, reason, expectedVersion), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Issue_returns_new_balance_and_version()
    {
        var result = await Handle(MovementType.Issue, 6);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.QuantityOnHand);
        Assert.Equal(3, result.Value.Version);
        Assert.True(result.Value.IsBelowMinimum);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task Issue_above_balance_breaks_a_business_rule_and_saves_nothing()
    {
        var result = await Handle(MovementType.Issue, 11);

        Assert.Equal(FailureKind.BusinessRule, result.Failure!.Kind);
        Assert.Equal(10, result.Failure.Details!["quantityOnHand"]);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Stale_expected_version_is_a_conflict_with_the_current_version()
    {
        var result = await Handle(MovementType.Issue, 1, expectedVersion: 1);

        Assert.Equal(FailureKind.Conflict, result.Failure!.Kind);
        Assert.Equal(2, result.Failure.Details!["currentVersion"]);
        Assert.Equal(10, _product.QuantityOnHand);
    }

    [Fact]
    public async Task Concurrent_write_detected_on_save_is_a_conflict()
    {
        _store.FailNextSaveWith = new ConcurrencyConflictException(new InvalidOperationException("0 rows affected"));

        var result = await Handle(MovementType.Issue, 1);

        Assert.Equal(FailureKind.Conflict, result.Failure!.Kind);
    }

    [Fact]
    public async Task Unknown_product_is_not_found()
    {
        var result = await CreateHandler().HandleAsync(
            new RecordMovementCommand(Guid.NewGuid(), MovementType.Receipt, 1, "Reason", 1), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, result.Failure!.Kind);
    }

    [Theory]
    [InlineData(MovementType.Receipt, 0, "Reason")]
    [InlineData(MovementType.Issue, -1, "Reason")]
    [InlineData(MovementType.Adjustment, -1, "Reason")]
    [InlineData(MovementType.Receipt, 1, "")]
    [InlineData((MovementType)99, 1, "Reason")]
    public async Task Invalid_command_is_a_validation_failure(MovementType type, int quantity, string reason)
    {
        var result = await Handle(type, quantity, reason: reason);

        Assert.Equal(FailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task Adjustment_to_zero_is_valid()
    {
        var result = await Handle(MovementType.Adjustment, 0, reason: "Cycle count");

        Assert.Equal(0, result.Value!.QuantityOnHand);
    }

    [Fact]
    public async Task Adjustment_that_does_not_change_the_balance_breaks_a_business_rule()
    {
        var result = await Handle(MovementType.Adjustment, 10, reason: "Cycle count");

        Assert.Equal(FailureKind.BusinessRule, result.Failure!.Kind);
    }
}
