using ErrorOr;
using Maroik.Core.Domain.Errors;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.ValueObjects;

/// <summary>
/// Value object that holds an asset's category: a member name of <see cref="AssetItemType"/> (e.g. "CashAsset"), the same
/// set the database's <c>Asset_Item_check</c> allows.
/// </summary>
public sealed class AssetItem : ValueObject
{
    /// <summary>The validated category name.</summary>
    public string Value { get; }

    /// <summary>Wraps an already-validated (or trusted) category; reached only through the factories.</summary>
    private AssetItem(string value) => Value = value;

    /// <summary>
    /// Re-creates an <see cref="AssetItem"/> from a value already stored in the database.
    /// Skips validation — only call this when the source is trusted (repository layer).
    /// </summary>
    internal static AssetItem FromTrustedSource(string value) => new(value);

    /// <summary>Creates an <see cref="AssetItem"/>; a validation error when the category is empty or not a known one.</summary>
    public static ErrorOr<AssetItem> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainError.Validation("Asset.ItemEmpty", "Asset category (item) cannot be empty.");

        // Constrain to the known taxonomy here rather than trusting the caller: otherwise the DB
        // Asset_Item_check constraint would be the only thing rejecting anything else, which
        // surfaces as a raw DB exception instead of this validation error.
        if (!AssetItems.IsKnown(value))
            return DomainError.Validation("Asset.ItemInvalid", "Asset category (item) is not a recognized value.");

        return new AssetItem(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
