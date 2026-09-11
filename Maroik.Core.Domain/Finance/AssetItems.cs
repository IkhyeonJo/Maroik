namespace Maroik.Core.Domain.Finance;

/// <summary>
/// Membership check for <see cref="AssetItemType"/>, the source of truth for
/// <see cref="Asset.Item"/> values — mirrors the DB's <c>Asset_Item_check</c> constraint.
/// Domain-validated by <see cref="Asset.Create"/> / <see cref="Asset.Update"/>; also exposed here so
/// the service layer's pre-repository-lookup short-circuit (<c>AssetService</c>) can check the same
/// allow-list without hand-duplicating the member list.
/// </summary>
public static class AssetItems
{
    /// <summary>Returns true when <paramref name="item"/> names a defined <see cref="AssetItemType"/> member.</summary>
    public static bool IsKnown(string? item) =>
        item != null && Enum.GetNames<AssetItemType>().Contains(item, StringComparer.Ordinal);
}
