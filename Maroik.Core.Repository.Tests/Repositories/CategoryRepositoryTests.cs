using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCategory = Maroik.Core.PostgreSQL.Models.Category;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CategoryRepository"/> using an EF Core InMemory database.
/// </summary>
public class CategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CategoryRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EfCategory MakeCategory(string name, string role = Role.User, long order = 1) => new()
    {
        Name = name,
        DisplayName = name,
        IconPath = "/icons/icon.svg",
        Controller = name,
        Action = "Index",
        Role = role,
        Order = order
    };

    private async Task SeedAsync(params EfCategory[] categories)
    {
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
    }

    // ── GetCategoryAsync ─────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetCategoryAsync</c> returns all categories.</summary>
    [Fact]
    public async Task GetCategoryAsync_ReturnsAllCategories()
    {
        await SeedAsync(
            MakeCategory("Dashboard", order: 1),
            MakeCategory("Finance", order: 2));

        IEnumerable<Category> result = await _sut.GetCategoryAsync();

        Assert.Equal(2, result.Count());
    }

    /// <summary>Verifies that <c>GetCategoryAsync</c> returns empty when no categories.</summary>
    [Fact]
    public async Task GetCategoryAsync_ReturnsEmpty_WhenNoCategories()
    {
        IEnumerable<Category> result = await _sut.GetCategoryAsync();

        Assert.Empty(result);
    }

    // ── GetCategoryByRoleAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>GetCategoryByRoleAsync</c> returns only categories for role.</summary>
    [Fact]
    public async Task GetCategoryByRoleAsync_ReturnsOnlyCategoriesForRole()
    {
        await SeedAsync(
            MakeCategory("Dashboard", role: Role.User, order: 1),
            MakeCategory("Admin", role: Role.Admin, order: 2),
            MakeCategory("Finance", role: Role.User, order: 3));

        IEnumerable<Category> result = await _sut.GetCategoryByRoleAsync(Role.User);

        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(Role.User, c.Role));
    }

    /// <summary>Verifies that <c>GetCategoryByRoleAsync</c> returns ordered by order.</summary>
    [Fact]
    public async Task GetCategoryByRoleAsync_ReturnsOrderedByOrder()
    {
        await SeedAsync(
            MakeCategory("Finance", role: Role.User, order: 2),
            MakeCategory("Dashboard", role: Role.User, order: 1));

        IEnumerable<Category> result = await _sut.GetCategoryByRoleAsync(Role.User);
        List<Category> list = result.ToList();

        Assert.Equal("Dashboard", list[0].Name);
        Assert.Equal("Finance", list[1].Name);
    }

    // ── CreateCategoryAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateCategoryAsync</c> adds category to database.</summary>
    [Fact]
    public async Task CreateCategoryAsync_AddsCategoryToDatabase()
    {
        var category = Category.Reconstitute(
            id: 0,
            name: "NewMenu",
            displayName: "New Menu",
            iconPath: "/icons/new.svg",
            controller: "NewMenu",
            action: "Index",
            role: Role.User,
            order: 5);

        await _sut.CreateCategoryAsync(category);

        EfCategory? saved = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "NewMenu", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(Role.User, saved.Role);
        Assert.Equal(5, saved.Order);
    }

    // ── UpdateCategoryAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateCategoryAsync</c> updates existing category.</summary>
    [Fact]
    public async Task UpdateCategoryAsync_UpdatesExistingCategory()
    {
        await SeedAsync(MakeCategory("OldName", order: 1));
        EfCategory efCategory = await _context.Categories.FirstAsync(TestContext.Current.CancellationToken);

        var category = Category.Reconstitute(
            id: efCategory.Id,
            name: "UpdatedName",
            displayName: "Updated Display",
            iconPath: "/icons/updated.svg",
            controller: "UpdatedName",
            action: "Index",
            role: Role.Admin,
            order: 10);

        await _sut.UpdateCategoryAsync(category);

        EfCategory? updated = await _context.Categories.FirstOrDefaultAsync(c => c.Id == efCategory.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedName", updated.Name);
        Assert.Equal(Role.Admin, updated.Role);
        Assert.Equal(10, updated.Order);
    }

    // ── DeleteCategoryAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteCategoryAsync</c> removes category from database.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategoryFromDatabase()
    {
        await SeedAsync(MakeCategory("ToDelete"));
        EfCategory efCategory = await _context.Categories.FirstAsync(TestContext.Current.CancellationToken);

        var category = Category.Reconstitute(
            id: efCategory.Id,
            name: efCategory.Name,
            displayName: efCategory.DisplayName,
            iconPath: efCategory.IconPath,
            controller: efCategory.Controller,
            action: efCategory.Action,
            role: efCategory.Role,
            order: efCategory.Order);

        await _sut.DeleteCategoryAsync(category);

        EfCategory? deleted = await _context.Categories.FirstOrDefaultAsync(c => c.Id == efCategory.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteCategoryAsync</c> does not affect other categories.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_DoesNotAffectOtherCategories()
    {
        await SeedAsync(MakeCategory("Cat1", order: 1), MakeCategory("Cat2", order: 2));
        List<EfCategory> all = await _context.Categories.ToListAsync(TestContext.Current.CancellationToken);

        var category = Category.Reconstitute(
            id: all[0].Id,
            name: all[0].Name,
            displayName: all[0].DisplayName,
            iconPath: all[0].IconPath,
            controller: all[0].Controller,
            action: all[0].Action,
            role: all[0].Role,
            order: all[0].Order);

        await _sut.DeleteCategoryAsync(category);

        List<EfCategory> remaining = await _context.Categories.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
