using Maroik.Core.Domain.Menu;

namespace Maroik.Core.DomainTests.Menu;

/// <summary>
/// Unit tests for <see cref="Category"/>.
/// Covers creation validation (name empty guard) and reconstitution from trusted data.
/// </summary>
public class CategoryTests
{
    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns category, when valid.</summary>
    [Fact]
    public void Create_ReturnsCategory_WhenValid()
    {
        var result = Category.Create(1, "Dashboard", "Dashboard", "/icons/dash.svg", "Home", "Index", "User", 1);

        Assert.False(result.IsError);
        Assert.Equal("Dashboard", result.Value.Name);
        Assert.Equal("Home", result.Value.Controller);
        Assert.Equal("Index", result.Value.Action);
        Assert.Equal("User", result.Value.Role);
        Assert.Equal(1, result.Value.Order);
    }

    /// <summary>Create returns error, when name empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenNameEmpty(string? name)
    {
        var result = Category.Create(1, name, "Dashboard", null, "Home", "Index", "User", 1);

        Assert.True(result.IsError);
        Assert.Equal("Category.NameEmpty", result.FirstError.Code);
    }

    // -- Reconstitute -----------------------------------------------------------

    /// <summary>Reconstitute rebuilds category without validation.</summary>
    [Fact]
    public void Reconstitute_RebuildsCategory_WithoutValidation()
    {
        var category = Category.Reconstitute(5, "Board", "Board", "/icons/board.svg", "Board", "List", "Admin", 2);

        Assert.Equal(5, category.Id);
        Assert.Equal("Board", category.Name);
        Assert.Equal("Board", category.DisplayName);
        Assert.Equal("/icons/board.svg", category.IconPath);
        Assert.Equal("Board", category.Controller);
        Assert.Equal("List", category.Action);
        Assert.Equal("Admin", category.Role);
        Assert.Equal(2, category.Order);
    }

    /// <summary>Reconstitute allows null name.</summary>
    [Fact]
    public void Reconstitute_AllowsNullName()
    {
        var category = Category.Reconstitute(1, null, null, null, null, null, null, 0);

        Assert.Null(category.Name);
    }
}
