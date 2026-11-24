namespace Inventory.IntegrationTests;

internal sealed record CreatedProduct(Guid Id);

internal sealed record ProductView(Guid Id, string Sku, string Name, int QuantityOnHand, int MinimumStock, bool IsBelowMinimum, int Version);

internal sealed record MovementResult(Guid MovementId, int QuantityOnHand, int Version, bool IsBelowMinimum);

internal sealed record MovementView(Guid Id, string Type, int Change, int BalanceAfter, string Reason);

internal sealed record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount);

internal sealed record Problem(int Status, string? Detail, Dictionary<string, string[]>? Errors, int? CurrentVersion, int? QuantityOnHand);
