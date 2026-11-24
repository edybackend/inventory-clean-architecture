using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Products;

public sealed record RegisterProductCommand(string Sku, string Name, int MinimumStock);

public sealed class RegisterProductValidator : AbstractValidator<RegisterProductCommand>
{
    public RegisterProductValidator()
    {
        RuleFor(c => c.Sku).Must(Domain.Sku.IsValid).WithMessage("SKU must have 3-20 letters, digits or dashes.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Product.MaxNameLength);
        RuleFor(c => c.MinimumStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class RegisterProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IValidator<RegisterProductCommand> validator,
    TimeProvider clock)
{
    public async Task<Result<Guid>> HandleAsync(RegisterProductCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToFailure();
        }

        var sku = Sku.Create(command.Sku);
        if (await products.SkuExistsAsync(sku, cancellationToken))
        {
            return DuplicateSku(sku);
        }

        var product = Product.Register(sku, command.Name, command.MinimumStock, clock.GetUtcNow());
        products.Add(product);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Two requests passed the check above at the same time; the unique index decided.
            return DuplicateSku(sku);
        }

        return Result.Success(product.Id);
    }

    private static Failure DuplicateSku(Sku sku) => new(FailureKind.Conflict, $"SKU {sku} already exists.");
}
