using Maroik.Core.Contract.Dtos;

namespace Maroik.WebSite.Utilities;

public static class DbCache
{
    public static IEnumerable<CategoryResponse> AdminCategories { get; set; } = null;
    public static IEnumerable<SubCategoryResponse> AdminSubCategories { get; set; } = null;

    public static IEnumerable<CategoryResponse> UserCategories { get; set; } = null;
    public static IEnumerable<SubCategoryResponse> UserSubCategories { get; set; } = null;

    public static IEnumerable<CategoryResponse> AnonymousCategories { get; set; } = null;
    public static IEnumerable<SubCategoryResponse> AnonymousSubCategories { get; set; } = null;
}