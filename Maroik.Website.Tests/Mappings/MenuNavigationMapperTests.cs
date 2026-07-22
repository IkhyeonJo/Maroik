using Maroik.Core.Contract.Dtos;
using Maroik.Website.Mappings;

namespace Maroik.Website.Tests.Mappings;

/// <summary>
/// Unit tests for <see cref="MenuNavigationMapper"/> — the single source of truth for
/// resolving which navigation Category/SubCategory matches the current controller/action.
/// This logic used to be triplicated (Admin/User/Anonymous) across <c>AuthorizationFilter</c>,
/// <c>_MainLeftSideBar.cshtml</c>, and <c>_BodyPageHeader.cshtml</c>.
/// </summary>
public class MenuNavigationMapperTests
{
    private static CategoryResponse SingleCategory(long id, string controller, string action, long order = 0) => new()
    {
        Id = id,
        DisplayName = $"Category{id}",
        Controller = controller,
        Action = action,
        Order = order
    };

    private static CategoryResponse ParentCategory(long id, string controller, long order = 0) => new()
    {
        Id = id,
        DisplayName = $"Category{id}",
        Controller = controller,
        Action = null,
        Order = order
    };

    private static SubCategoryResponse SubCategory(long id, long categoryId, string action, long order = 0) => new()
    {
        Id = id,
        CategoryId = categoryId,
        DisplayName = $"SubCategory{id}",
        Action = action,
        Order = order
    };

    // -- ResolveActiveMenuItem ----------------------------------------------------

    [Fact]
    public void ResolveActiveMenuItem_SingleCategoryMatches_ReturnsMatchWithNoSubCategory()
    {
        var categories = new[] { SingleCategory(1, "Dashboard", "UserIndex") };

        var match = categories.ResolveActiveMenuItem([], "Dashboard", "UserIndex");

        Assert.True(match.IsMatch);
        Assert.Equal(1, match.Category!.Id);
        Assert.Null(match.SubCategory);
    }

    [Fact]
    public void ResolveActiveMenuItem_SubCategoryMatches_ReturnsMatchWithBothCategoryAndSubCategory()
    {
        var categories = new[] { ParentCategory(1, "AccountBook") };
        var subCategories = new[] { SubCategory(10, categoryId: 1, action: "Income") };

        var match = categories.ResolveActiveMenuItem(subCategories, "AccountBook", "Income");

        Assert.True(match.IsMatch);
        Assert.Equal(1, match.Category!.Id);
        Assert.Equal(10, match.SubCategory!.Id);
    }

    [Fact]
    public void ResolveActiveMenuItem_NoMatch_ReturnsIsMatchFalse()
    {
        var categories = new[] { SingleCategory(1, "Dashboard", "UserIndex") };

        var match = categories.ResolveActiveMenuItem([], "Forum", "FreeForum");

        Assert.False(match.IsMatch);
        Assert.Null(match.Category);
        Assert.Null(match.SubCategory);
    }

    [Fact]
    public void ResolveActiveMenuItem_SubCategoryBelongsToDifferentCategory_DoesNotMatch()
    {
        var categories = new[] { ParentCategory(1, "AccountBook") };
        // SubCategory's CategoryId (2) does not match the parent category's Id (1).
        var subCategories = new[] { SubCategory(10, categoryId: 2, action: "Income") };

        var match = categories.ResolveActiveMenuItem(subCategories, "AccountBook", "Income");

        Assert.False(match.IsMatch);
    }

    [Fact]
    public void ResolveActiveMenuItem_ActionMatchesButControllerDoesNot_DoesNotMatch()
    {
        var categories = new[] { SingleCategory(1, "Dashboard", "UserIndex") };

        var match = categories.ResolveActiveMenuItem([], "Forum", "UserIndex");

        Assert.False(match.IsMatch);
    }

    [Fact]
    public void ResolveActiveMenuItem_EmptyCategories_ReturnsIsMatchFalse()
    {
        var match = Array.Empty<CategoryResponse>().ResolveActiveMenuItem([], "Dashboard", "UserIndex");

        Assert.False(match.IsMatch);
    }

    // -- ToSidebarMenu -------------------------------------------------------------

    [Fact]
    public void ToSidebarMenu_SortsCategoriesByOrder()
    {
        var categories = new[]
        {
            SingleCategory(1, "Second", "Index", order: 2),
            SingleCategory(2, "First", "Index", order: 1)
        };

        var menu = categories.ToSidebarMenu([], "None", "None");

        Assert.Equal(2, menu.Categories[0].Category.Id);
        Assert.Equal(1, menu.Categories[1].Category.Id);
    }

    [Fact]
    public void ToSidebarMenu_SingleCategory_IsActiveWhenControllerAndActionMatch()
    {
        var categories = new[] { SingleCategory(1, "Dashboard", "UserIndex") };

        var menu = categories.ToSidebarMenu([], "Dashboard", "UserIndex");

        Assert.True(menu.Categories[0].IsActive);
        Assert.Empty(menu.Categories[0].SubCategories);
    }

    [Fact]
    public void ToSidebarMenu_SingleCategory_NotActiveWhenRouteDiffers()
    {
        var categories = new[] { SingleCategory(1, "Dashboard", "UserIndex") };

        var menu = categories.ToSidebarMenu([], "Forum", "FreeForum");

        Assert.False(menu.Categories[0].IsActive);
    }

    [Fact]
    public void ToSidebarMenu_ParentCategory_IsActiveWhenAnyChildMatches()
    {
        var categories = new[] { ParentCategory(1, "AccountBook") };
        var subCategories = new[]
        {
            SubCategory(10, categoryId: 1, action: "Income", order: 1),
            SubCategory(11, categoryId: 1, action: "Expenditure", order: 2)
        };

        var menu = categories.ToSidebarMenu(subCategories, "AccountBook", "Expenditure");

        Assert.True(menu.Categories[0].IsActive);
        Assert.False(menu.Categories[0].SubCategories.Single(s => s.SubCategory.Id == 10).IsActive);
        Assert.True(menu.Categories[0].SubCategories.Single(s => s.SubCategory.Id == 11).IsActive);
    }

    [Fact]
    public void ToSidebarMenu_ParentCategory_NotActiveWhenNoChildMatches()
    {
        var categories = new[] { ParentCategory(1, "AccountBook") };
        var subCategories = new[] { SubCategory(10, categoryId: 1, action: "Income") };

        var menu = categories.ToSidebarMenu(subCategories, "AccountBook", "Asset");

        Assert.False(menu.Categories[0].IsActive);
        Assert.All(menu.Categories[0].SubCategories, sub => Assert.False(sub.IsActive));
    }

    [Fact]
    public void ToSidebarMenu_SubCategories_SortedByOrder()
    {
        var categories = new[] { ParentCategory(1, "AccountBook") };
        var subCategories = new[]
        {
            SubCategory(10, categoryId: 1, action: "Second", order: 2),
            SubCategory(11, categoryId: 1, action: "First", order: 1)
        };

        var menu = categories.ToSidebarMenu(subCategories, "None", "None");

        Assert.Equal(11, menu.Categories[0].SubCategories[0].SubCategory.Id);
        Assert.Equal(10, menu.Categories[0].SubCategories[1].SubCategory.Id);
    }
}
