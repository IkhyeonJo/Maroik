namespace Maroik.Website.Constants;

/// <summary>
/// Distributed-cache keys used by <see cref="Maroik.Website.Filters.AuthorizationFilter"/> to store
/// role-partitioned navigation menu data (categories and sub-categories).
/// </summary>
internal static class NavigationCacheKeys
{
    internal const string AdminCategories = "nav_admin_categories";
    internal const string AdminSubCategories = "nav_admin_subcategories";
    internal const string UserCategories = "nav_user_categories";
    internal const string UserSubCategories = "nav_user_subcategories";
    internal const string AnonymousCategories = "nav_anonymous_categories";
    internal const string AnonymousSubCategories = "nav_anonymous_subcategories";
}
