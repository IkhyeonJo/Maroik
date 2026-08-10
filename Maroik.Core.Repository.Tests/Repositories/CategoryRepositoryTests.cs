using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Menu;
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

    // -- Helpers --------------------------------------------------------------

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

    // -- GetAllAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>GetAllAsync</c> returns all categories.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        await SeedAsync(
            MakeCategory("Dashboard", order: 1),
            MakeCategory("Finance", order: 2));

        IEnumerable<Category> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count());
    }

    /// <summary>Verifies that <c>GetAllAsync</c> returns empty when no categories.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoCategories()
    {
        IEnumerable<Category> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateCategoryAsync --------------------------------------------------

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

        await _sut.CreateAsync(category, TestContext.Current.CancellationToken);

        EfCategory? saved = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "NewMenu", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(Role.User, saved.Role);
        Assert.Equal(5, saved.Order);
    }

    // -- UpdateEntityAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing category.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingCategory()
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

        await _sut.UpdateEntityAsync(category, TestContext.Current.CancellationToken);

        EfCategory? updated = await _context.Categories.FirstOrDefaultAsync(c => c.Id == efCategory.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedName", updated.Name);
        Assert.Equal(Role.Admin, updated.Role);
        Assert.Equal(10, updated.Order);
    }

    // -- DeleteByIdAsync --------------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes category from database.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategoryFromDatabase()
    {
        await SeedAsync(MakeCategory("ToDelete"));
        EfCategory efCategory = await _context.Categories.FirstAsync(TestContext.Current.CancellationToken);

        await _sut.DeleteByIdAsync(efCategory.Id, TestContext.Current.CancellationToken);

        EfCategory? deleted = await _context.Categories.FirstOrDefaultAsync(c => c.Id == efCategory.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other categories.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_DoesNotAffectOtherCategories()
    {
        await SeedAsync(MakeCategory("Cat1", order: 1), MakeCategory("Cat2", order: 2));
        List<EfCategory> all = await _context.Categories.ToListAsync(TestContext.Current.CancellationToken);

        await _sut.DeleteByIdAsync(all[0].Id, TestContext.Current.CancellationToken);

        List<EfCategory> remaining = await _context.Categories.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
