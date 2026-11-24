using Inventory.Application.Movements;
using Inventory.Application.Products;
using Inventory.Application.Queries;
using Inventory.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Inventory.Api.Endpoints;

internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products");

        products.MapPost("/", RegisterAsync)
            .WithName("RegisterProduct")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        products.MapGet("/", ListAsync).WithName("ListProducts");
        products.MapGet("/{id:guid}", GetAsync).WithName("GetProduct");

        products.MapPost("/{id:guid}/movements", RecordMovementAsync)
            .WithName("RecordMovement")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        products.MapGet("/{id:guid}/movements", ListMovementsAsync).WithName("ListMovements");

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterProductCommand command, RegisterProductHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new RegisterProductResponse(result.Value), "GetProduct", new { id = result.Value })
            : result.Failure!.ToProblem();
    }

    private static async Task<Ok<PagedResult<ProductSummary>>> ListAsync(
        IProductQueries queries, CancellationToken ct, string? search = null, bool belowMinimum = false, int? page = null, int? pageSize = null) =>
        TypedResults.Ok(await queries.ListAsync(new ProductFilter(search, belowMinimum, new PageRequest(page, pageSize)), ct));

    private static async Task<Results<Ok<ProductDetails>, NotFound>> GetAsync(Guid id, IProductQueries queries, CancellationToken ct) =>
        await queries.GetByIdAsync(id, ct) is { } product ? TypedResults.Ok(product) : TypedResults.NotFound();

    private static async Task<IResult> RecordMovementAsync(Guid id, RecordMovementRequest request, RecordMovementHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new RecordMovementCommand(id, request.Type, request.Quantity, request.Reason, request.ExpectedVersion), ct);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure!.ToProblem();
    }

    private static async Task<Results<Ok<PagedResult<MovementView>>, NotFound>> ListMovementsAsync(
        Guid id, IProductQueries queries, CancellationToken ct, int? page = null, int? pageSize = null) =>
        await queries.ListMovementsAsync(id, new PageRequest(page, pageSize), ct) is { } movements
            ? TypedResults.Ok(movements)
            : TypedResults.NotFound();
}

internal sealed record RegisterProductResponse(Guid Id);

/// <summary>Body of POST /api/products/{id}/movements. The product id comes from the route.</summary>
internal sealed record RecordMovementRequest(MovementType Type, int Quantity, string Reason, int ExpectedVersion);
