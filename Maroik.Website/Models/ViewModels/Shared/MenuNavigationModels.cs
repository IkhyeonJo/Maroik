using Maroik.Core.Contract.Dtos;

namespace Maroik.Website.Models.ViewModels.Shared;

/// <summary>Result of matching the current controller/action against a role's Category/SubCategory lists.</summary>
public sealed record ActiveMenuMatch(bool IsMatch, CategoryResponse? Category, SubCategoryResponse? SubCategory);

/// <summary>A sub-menu entry annotated with whether it is the currently active route.</summary>
public sealed class SidebarSubCategoryItem
{
    public required SubCategoryResponse SubCategory { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>A top-level menu entry annotated with whether it (or one of its children) is the currently active route.</summary>
public sealed class SidebarCategoryItem
{
    public required CategoryResponse Category { get; init; }
    public bool IsActive { get; init; }
    public required IReadOnlyList<SidebarSubCategoryItem> SubCategories { get; init; }
}

/// <summary>The fully resolved, render-ready sidebar menu tree for one role.</summary>
public sealed class SidebarMenuViewModel
{
    public required IReadOnlyList<SidebarCategoryItem> Categories { get; init; }
}
