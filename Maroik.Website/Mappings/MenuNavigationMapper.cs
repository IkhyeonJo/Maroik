using Maroik.Core.Contract.Dtos;
using Maroik.Website.Models.ViewModels.Shared;

namespace Maroik.Website.Mappings;

/// <summary>
/// Resolves which navigation Category/SubCategory matches the current controller/action.
/// Single source of truth for the matching algorithm previously duplicated across
/// <c>AuthorizationFilter</c>, <c>_MainLeftSideBar.cshtml</c> and <c>_BodyPageHeader.cshtml</c>.
/// </summary>
public static class MenuNavigationMapper
{
    extension(IEnumerable<CategoryResponse> categories)
    {
        /// <summary>
        /// Finds the first Category (and, for multi-level categories, the matching SubCategory)
        /// whose Controller/Action matches the current route. Mirrors the same two-part predicate
        /// (single categories have a direct Action; multi-level categories match via their children)
        /// that <c>AuthorizationFilter</c> used to gate access, so allow/deny outcomes are unchanged.
        /// </summary>
        public ActiveMenuMatch ResolveActiveMenuItem(
            IEnumerable<SubCategoryResponse> subCategories, string controllerName, string actionName)
        {
            var subCategoryList = subCategories as IReadOnlyCollection<SubCategoryResponse> ?? subCategories.ToList();

            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.Action))
                {
                    if (category.Controller == controllerName && category.Action == actionName)
                    {
                        return new ActiveMenuMatch(true, category, null);
                    }
                }
                else
                {
                    var matchedSubCategory = subCategoryList.FirstOrDefault(subCategory =>
                        subCategory.CategoryId == category.Id &&
                        category.Controller == controllerName &&
                        subCategory.Action == actionName);

                    if (matchedSubCategory != null)
                    {
                        return new ActiveMenuMatch(true, category, matchedSubCategory);
                    }
                }
            }

            return new ActiveMenuMatch(false, null, null);
        }

        /// <summary>
        /// Builds the full, sorted, active-flagged sidebar tree for rendering.
        /// Uses the same per-item predicate as <see cref="ResolveActiveMenuItem"/>, but annotates
        /// every category/sub-category instead of stopping at the first match.
        /// </summary>
        public SidebarMenuViewModel ToSidebarMenu(
            IEnumerable<SubCategoryResponse> subCategories, string controllerName, string actionName)
        {
            var subCategoryList = subCategories.ToList();

            var items = categories
                .OrderBy(category => category.Order)
                .Select(category =>
                {
                    var isSingleCategory = !string.IsNullOrEmpty(category.Action);
                    if (isSingleCategory)
                    {
                        return new SidebarCategoryItem
                        {
                            Category = category,
                            IsActive = category.Controller == controllerName && category.Action == actionName,
                            SubCategories = []
                        };
                    }

                    var subItems = subCategoryList
                        .Where(subCategory => subCategory.CategoryId == category.Id)
                        .OrderBy(subCategory => subCategory.Order)
                        .Select(subCategory => new SidebarSubCategoryItem
                        {
                            SubCategory = subCategory,
                            IsActive = category.Controller == controllerName && subCategory.Action == actionName
                        })
                        .ToList();

                    return new SidebarCategoryItem
                    {
                        Category = category,
                        IsActive = subItems.Any(subItem => subItem.IsActive),
                        SubCategories = subItems
                    };
                })
                .ToList();

            return new SidebarMenuViewModel { Categories = items };
        }
    }
}
