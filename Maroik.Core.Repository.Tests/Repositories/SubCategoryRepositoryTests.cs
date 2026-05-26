using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCategory = Maroik.Core.PostgreSQL.Models.Category;
using EfSubCategory = Maroik.Core.PostgreSQL.Models.SubCategory;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="SubCategoryRepository"/> using an EF Core InMemory database.
/// </summary>
public class SubCategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SubCategoryRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public SubCategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new SubCategoryRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EfSubCategory MakeSubCategory(long categoryId, string name, string role = Role.User, long order = 1) => new()
    {
        CategoryId = categoryId,
        Name = name,
        DisplayName = name,
        IconPath = "/icons/sub.svg",
        Action = name,
        Role = role,
        Order = order
    };

    private async Task<long> SeedCategoryAsync()
    {
        var category = new EfCategory
        {
            Name = "ParentCategory",
            DisplayName = "Parent",
            Controller = "ParentCategory",
            IconPath = "/icons/icon.svg",
            Role = Role.User,
            Order = 1
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category.Id;
    }

    private async Task SeedAsync(params EfSubCategory[] items)
    {
        await _context.SubCategories.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    // ── GetSubCategoryAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetSubCategoryAsync</c> returns all sub categories.</summary>
    [Fact]
    public async Task GetSubCategoryAsync_ReturnsAllSubCategories()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "SubA", order: 1),
            MakeSubCategory(catId, "SubB", order: 2));

        IEnumerable<SubCategory> result = await _sut.GetSubCategoryAsync();

        Assert.Equal(2, result.Count());
    }

    /// <summary>Verifies that <c>GetSubCategoryAsync</c> returns empty when no sub categories.</summary>
    [Fact]
    public async Task GetSubCategoryAsync_ReturnsEmpty_WhenNoSubCategories()
    {
        IEnumerable<SubCategory> result = await _sut.GetSubCategoryAsync();

        Assert.Empty(result);
    }

    // ── GetSubCategoryByRoleAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>GetSubCategoryByRoleAsync</c> returns only sub categories for role.</summary>
    [Fact]
    public async Task GetSubCategoryByRoleAsync_ReturnsOnlySubCategoriesForRole()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "UserSub1", role: Role.User, order: 1),
            MakeSubCategory(catId, "AdminSub", role: Role.Admin, order: 2),
            MakeSubCategory(catId, "UserSub2", role: Role.User, order: 3));

        IEnumerable<SubCategory> result = await _sut.GetSubCategoryByRoleAsync(Role.User);

        Assert.Equal(2, result.Count());
        Assert.All(result, sc => Assert.Equal(Role.User, sc.Role));
    }

    /// <summary>Verifies that <c>GetSubCategoryByRoleAsync</c> returns ordered by order.</summary>
    [Fact]
    public async Task GetSubCategoryByRoleAsync_ReturnsOrderedByOrder()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "SecondItem", role: Role.User, order: 2),
            MakeSubCategory(catId, "FirstItem", role: Role.User, order: 1));

        IEnumerable<SubCategory> result = await _sut.GetSubCategoryByRoleAsync(Role.User);
        List<SubCategory> list = result.ToList();

        Assert.Equal("FirstItem", list[0].Name);
        Assert.Equal("SecondItem", list[1].Name);
    }

    // ── CreateSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateSubCategoryAsync</c> adds sub category to database.</summary>
    [Fact]
    public async Task CreateSubCategoryAsync_AddsSubCategoryToDatabase()
    {
        long catId = await SeedCategoryAsync();

        var subCategory = SubCategory.Reconstitute(
            id: 0,
            categoryId: catId,
            name: "NewSubMenu",
            displayName: "New Sub Menu",
            iconPath: "/icons/new.svg",
            action: "NewAction",
            role: Role.User,
            order: 5);

        await _sut.CreateSubCategoryAsync(subCategory);

        EfSubCategory? saved = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "NewSubMenu", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(catId, saved.CategoryId);
        Assert.Equal(Role.User, saved.Role);
    }

    // ── UpdateSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateSubCategoryAsync</c> updates existing sub category.</summary>
    [Fact]
    public async Task UpdateSubCategoryAsync_UpdatesExistingSubCategory()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(MakeSubCategory(catId, "OldSub", order: 1));
        EfSubCategory efSub = await _context.SubCategories.FirstAsync(TestContext.Current.CancellationToken);

        var subCategory = SubCategory.Reconstitute(
            id: efSub.Id,
            categoryId: catId,
            name: "UpdatedSub",
            displayName: "Updated Sub",
            iconPath: "/icons/updated.svg",
            action: "UpdatedAction",
            role: Role.Admin,
            order: 10);

        await _sut.UpdateSubCategoryAsync(subCategory);

        EfSubCategory? updated = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Id == efSub.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedSub", updated.Name);
        Assert.Equal(Role.Admin, updated.Role);
        Assert.Equal(10, updated.Order);
    }

    // ── DeleteSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteSubCategoryAsync</c> removes sub category from database.</summary>
    [Fact]
    public async Task DeleteSubCategoryAsync_RemovesSubCategoryFromDatabase()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(MakeSubCategory(catId, "ToDelete"));
        EfSubCategory efSub = await _context.SubCategories.FirstAsync(TestContext.Current.CancellationToken);

        var subCategory = SubCategory.Reconstitute(
            id: efSub.Id,
            categoryId: efSub.CategoryId,
            name: efSub.Name,
            displayName: efSub.DisplayName,
            iconPath: efSub.IconPath,
            action: efSub.Action,
            role: efSub.Role,
            order: efSub.Order);

        await _sut.DeleteSubCategoryAsync(subCategory);

        EfSubCategory? deleted = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Id == efSub.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteSubCategoryAsync</c> does not affect other sub categories.</summary>
    [Fact]
    public async Task DeleteSubCategoryAsync_DoesNotAffectOtherSubCategories()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "Sub1", order: 1),
            MakeSubCategory(catId, "Sub2", order: 2));
        List<EfSubCategory> all = await _context.SubCategories.ToListAsync(TestContext.Current.CancellationToken);

        var subCategory = SubCategory.Reconstitute(
            id: all[0].Id,
            categoryId: all[0].CategoryId,
            name: all[0].Name,
            displayName: all[0].DisplayName,
            iconPath: all[0].IconPath,
            action: all[0].Action,
            role: all[0].Role,
            order: all[0].Order);

        await _sut.DeleteSubCategoryAsync(subCategory);

        List<EfSubCategory> remaining = await _context.SubCategories.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
