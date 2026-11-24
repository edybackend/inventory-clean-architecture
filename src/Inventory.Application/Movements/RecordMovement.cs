using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Movements;

/// <param name="Quantity">Units to receive or issue; for an adjustment, the counted quantity.</param>
/// <param name="ExpectedVersion">The product version the client read. A different current version means someone changed it first.</param>
public sealed record RecordMovementCommand(Guid ProductId, MovementType Type, int Quantity, string Reason, int ExpectedVersion);

public sealed record MovementRecorded(Guid MovementId, int QuantityOnHand, int Version, bool IsBelowMinimum);

public sealed class RecordMovementValidator : AbstractValidator<RecordMovementCommand>
{
    public RecordMovementValidator()
    {
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Quantity).GreaterThan(0).When(c => c.Type is not MovementType.Adjustment);
        RuleFor(c => c.Quantity).GreaterThanOrEqualTo(0).When(c => c.Type is MovementType.Adjustment);
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(StockMovement.MaxReasonLength);
        RuleFor(c => c.ExpectedVersion).GreaterThan(0);
    }
}

public sealed partial class RecordMovementHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IValidator<RecordMovementCommand> validator,
    TimeProvider clock,
    ILogger<RecordMovementHandler> logger)
{
    public async Task<Result<MovementRecorded>> HandleAsync(RecordMovementCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToFailure();
        }

        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return new Failure(FailureKind.NotFound, $"Product {command.ProductId} was not found.");
        }

        // First guard: the client saw an older version (someone changed the product while the screen was open).
        if (product.Version != command.ExpectedVersion)
        {
            return StaleVersion(product.Version);
        }

        StockMovement movement;
        try
        {
            var now = clock.GetUtcNow();
            movement = command.Type switch
            {
                MovementType.Receipt => product.Receive(command.Quantity, command.Reason, now),
                MovementType.Issue => product.Issue(command.Quantity, command.Reason, now),
                MovementType.Adjustment => product.Adjust(command.Quantity, command.Reason, now),
                _ => throw new ArgumentOutOfRangeException(nameof(command), command.Type, "Unknown movement type."),
            };
        }
        catch (InsufficientStockException ex)
        {
            return new Failure(FailureKind.BusinessRule, ex.Message, Details: new Dictionary<string, object?> { ["quantityOnHand"] = ex.OnHand });
        }
        catch (DomainException ex)
        {
            return new Failure(FailureKind.BusinessRule, ex.Message);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Second guard: another request read the same version and saved first (UPDATE ... WHERE Version = @read).
            LogConcurrencyConflict(command.ProductId, command.ExpectedVersion);
            return StaleVersion(currentVersion: null);
        }

        return Result.Success(new MovementRecorded(movement.Id, product.QuantityOnHand, product.Version, product.IsBelowMinimum));
    }

    private static Failure StaleVersion(int? currentVersion) => new(
        FailureKind.Conflict,
        "Stock changed since you opened this product. Reload and try again.",
        Details: new Dictionary<string, object?> { ["currentVersion"] = currentVersion });

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrent update on product {ProductId} (expected version {ExpectedVersion})")]
    private partial void LogConcurrencyConflict(Guid productId, int expectedVersion);
}
