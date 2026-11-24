using Inventory.Application;
using Inventory.Application.Abstractions;
using Inventory.Application.Products;

namespace Inventory.UnitTests;

public class RegisterProductHandlerTests
{
    private readonly InMemoryProducts _store = new();

    private RegisterProductHandler CreateHandler() =>
        new(_store, _store, new RegisterProductValidator(), new FixedClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Valid_command_registers_the_product()
    {
        var result = await CreateHandler().HandleAsync(new RegisterProductCommand("kb-100", "Keyboard", 5), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var product = await _store.GetByIdAsync(result.Value, TestContext.Current.CancellationToken);
        Assert.Equal("KB-100", product!.Sku.Value);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task Invalid_command_lists_every_invalid_field_and_saves_nothing()
    {
        var result = await CreateHandler().HandleAsync(new RegisterProductCommand("x", "", -1), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Validation, result.Failure!.Kind);
        Assert.Equal(["minimumStock", "name", "sku"], result.Failure.Fields!.Keys.Order());
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Duplicate_sku_in_any_case_is_a_conflict()
    {
        var handler = CreateHandler();
        await handler.HandleAsync(new RegisterProductCommand("KB-100", "Keyboard", 5), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(new RegisterProductCommand("kb-100", "Other", 0), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Conflict, result.Failure!.Kind);
    }

    [Fact]
    public async Task Unique_index_violation_on_save_is_a_conflict()
    {
        _store.FailNextSaveWith = new DuplicateKeyException(new InvalidOperationException("UNIQUE constraint failed"));

        var result = await CreateHandler().HandleAsync(new RegisterProductCommand("KB-100", "Keyboard", 5), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Conflict, result.Failure!.Kind);
    }
}
