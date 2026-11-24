using FluentValidation.Results;

namespace Inventory.Application;

internal static class ValidationExtensions
{
    /// <summary>Converts FluentValidation failures to a validation <see cref="Failure"/> keyed by camelCase field name.</summary>
    public static Failure ToFailure(this ValidationResult validation) => new(
        FailureKind.Validation,
        "One or more fields are invalid.",
        validation.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()));

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
