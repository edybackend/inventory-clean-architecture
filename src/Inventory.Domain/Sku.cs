using System.Text.RegularExpressions;

namespace Inventory.Domain;

/// <summary>Stock keeping unit: 3-20 upper-case letters, digits or dashes. Compared by value.</summary>
public sealed partial record Sku
{
    public const int MaxLength = 20;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? value) => Pattern().IsMatch(Normalize(value));

    public static Sku Create(string? value)
    {
        var normalized = Normalize(value);
        if (!Pattern().IsMatch(normalized))
        {
            throw new DomainException("SKU must have 3-20 letters, digits or dashes.");
        }

        return new Sku(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    [GeneratedRegex("^[A-Z0-9-]{3,20}$")]
    private static partial Regex Pattern();
}
