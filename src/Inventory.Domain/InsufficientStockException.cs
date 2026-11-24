namespace Inventory.Domain;

/// <summary>An issue asked for more units than the product has on hand.</summary>
public sealed class InsufficientStockException(int requested, int onHand)
    : Exception($"Cannot issue {requested} unit(s): only {onHand} on hand.")
{
    public int Requested { get; } = requested;

    public int OnHand { get; } = onHand;
}
