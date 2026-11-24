namespace Inventory.Domain;

/// <summary>One line of the append-only stock history. Created only by <see cref="Product"/>; never updated.</summary>
public sealed class StockMovement
{
    public const int MaxReasonLength = 200;

    private StockMovement()
    {
        Reason = string.Empty; // required by EF Core
    }

    internal StockMovement(Guid productId, MovementType type, int change, int balanceAfter, string reason, DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7(occurredAt);
        ProductId = productId;
        Type = type;
        Change = change;
        BalanceAfter = balanceAfter;
        Reason = reason;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public MovementType Type { get; private set; }

    /// <summary>Signed change in units: positive for receipts, negative for issues, either for adjustments.</summary>
    public int Change { get; private set; }

    public int BalanceAfter { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
