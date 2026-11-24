namespace Inventory.Domain;

/// <summary>
/// Aggregate root. The quantity on hand changes only through movements, and every movement is recorded,
/// so the balance always equals the sum of the history. <see cref="Version"/> grows with each change and is
/// used for optimistic concurrency.
/// </summary>
public sealed class Product
{
    public const int MaxNameLength = 100;

    private readonly List<StockMovement> _newMovements = [];

    private Product()
    {
        Sku = null!; // required by EF Core
        Name = string.Empty;
    }

    private Product(Guid id, Sku sku, string name, int minimumStock, DateTimeOffset createdAt)
    {
        Id = id;
        Sku = sku;
        Name = name;
        MinimumStock = minimumStock;
        Version = 1;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Sku Sku { get; private set; }

    public string Name { get; private set; }

    public int MinimumStock { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsBelowMinimum => QuantityOnHand < MinimumStock;

    /// <summary>
    /// Movements recorded since the aggregate was loaded. The full history is not loaded to change stock:
    /// the balance is kept on the product, and the history is only appended to.
    /// </summary>
    public IReadOnlyList<StockMovement> NewMovements => _newMovements.AsReadOnly();

    public static Product Register(Sku sku, string name, int minimumStock, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sku);

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            throw new DomainException($"Name is required and must have at most {MaxNameLength} characters.");
        }

        if (minimumStock < 0)
        {
            throw new DomainException("Minimum stock cannot be negative.");
        }

        return new Product(Guid.CreateVersion7(now), sku, name.Trim(), minimumStock, now);
    }

    public StockMovement Receive(int quantity, string reason, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        return Record(MovementType.Receipt, quantity, reason, now);
    }

    public StockMovement Issue(int quantity, string reason, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        if (quantity > QuantityOnHand)
        {
            throw new InsufficientStockException(quantity, QuantityOnHand);
        }

        return Record(MovementType.Issue, -quantity, reason, now);
    }

    /// <summary>Sets the balance to the physically counted quantity; the movement stores the difference.</summary>
    public StockMovement Adjust(int countedQuantity, string reason, DateTimeOffset now)
    {
        if (countedQuantity < 0)
        {
            throw new DomainException("Counted quantity cannot be negative.");
        }

        var change = countedQuantity - QuantityOnHand;
        if (change == 0)
        {
            throw new DomainException($"The balance is already {QuantityOnHand}; an adjustment must change it.");
        }

        return Record(MovementType.Adjustment, change, reason, now);
    }

    private StockMovement Record(MovementType type, int change, string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > StockMovement.MaxReasonLength)
        {
            throw new DomainException($"Reason is required and must have at most {StockMovement.MaxReasonLength} characters.");
        }

        QuantityOnHand += change;
        Version++;

        var movement = new StockMovement(Id, type, change, QuantityOnHand, reason.Trim(), now);
        _newMovements.Add(movement);
        return movement;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }
}
