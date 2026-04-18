using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

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

    private static SubCategory MakeSubCategory(long categoryId, string name, string role = "User", long order = 1) => new()
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
        var category = new Category
        {
            Name = "ParentCategory",
            DisplayName = "Parent",
            Role = "User",
            Order = 1
        };
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category.Id;
    }

    private async Task SeedAsync(params SubCategory[] items)
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

        IEnumerable<SubCategoryResponse> result = await _sut.GetSubCategoryAsync();

        Assert.Equal(2, result.Count());
    }

    /// <summary>Verifies that <c>GetSubCategoryAsync</c> returns empty when no sub categories.</summary>
    [Fact]
    public async Task GetSubCategoryAsync_ReturnsEmpty_WhenNoSubCategories()
    {
        IEnumerable<SubCategoryResponse> result = await _sut.GetSubCategoryAsync();

        Assert.Empty(result);
    }

    // ── GetSubCategoryByRoleAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>GetSubCategoryByRoleAsync</c> returns only sub categories for role.</summary>
    [Fact]
    public async Task GetSubCategoryByRoleAsync_ReturnsOnlySubCategoriesForRole()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "UserSub1", role: "User", order: 1),
            MakeSubCategory(catId, "AdminSub", role: "Admin", order: 2),
            MakeSubCategory(catId, "UserSub2", role: "User", order: 3));

        IEnumerable<SubCategoryResponse> result = await _sut.GetSubCategoryByRoleAsync("User");

        Assert.Equal(2, result.Count());
        Assert.All(result, sc => Assert.Equal("User", sc.Role));
    }

    /// <summary>Verifies that <c>GetSubCategoryByRoleAsync</c> returns ordered by order.</summary>
    [Fact]
    public async Task GetSubCategoryByRoleAsync_ReturnsOrderedByOrder()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(
            MakeSubCategory(catId, "SecondItem", role: "User", order: 2),
            MakeSubCategory(catId, "FirstItem", role: "User", order: 1));

        IEnumerable<SubCategoryResponse> result = await _sut.GetSubCategoryByRoleAsync("User");
        List<SubCategoryResponse> list = result.ToList();

        Assert.Equal("FirstItem", list[0].Name);
        Assert.Equal("SecondItem", list[1].Name);
    }

    // ── CreateSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateSubCategoryAsync</c> adds sub category to database.</summary>
    [Fact]
    public async Task CreateSubCategoryAsync_AddsSubCategoryToDatabase()
    {
        long catId = await SeedCategoryAsync();

        var request = new SubCategoryRequest
        {
            CategoryId = catId,
            Name = "NewSubMenu",
            DisplayName = "New Sub Menu",
            IconPath = "/icons/new.svg",
            Action = "NewAction",
            Role = "User",
            Order = 5
        };

        await _sut.CreateSubCategoryAsync(request);

        SubCategory? saved = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "NewSubMenu", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(catId, saved.CategoryId);
        Assert.Equal("User", saved.Role);
    }

    // ── UpdateSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateSubCategoryAsync</c> updates existing sub category.</summary>
    [Fact]
    public async Task UpdateSubCategoryAsync_UpdatesExistingSubCategory()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(MakeSubCategory(catId, "OldSub", order: 1));
        SubCategory sub = await _context.SubCategories.FirstAsync(TestContext.Current.CancellationToken);

        var request = new SubCategoryRequest
        {
            Id = sub.Id,
            CategoryId = catId,
            Name = "UpdatedSub",
            DisplayName = "Updated Sub",
            IconPath = "/icons/updated.svg",
            Action = "UpdatedAction",
            Role = "Admin",
            Order = 10
        };

        await _sut.UpdateSubCategoryAsync(request);

        SubCategory? updated = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Id == sub.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedSub", updated.Name);
        Assert.Equal("Admin", updated.Role);
        Assert.Equal(10, updated.Order);
    }

    // ── DeleteSubCategoryAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteSubCategoryAsync</c> removes sub category from database.</summary>
    [Fact]
    public async Task DeleteSubCategoryAsync_RemovesSubCategoryFromDatabase()
    {
        long catId = await SeedCategoryAsync();
        await SeedAsync(MakeSubCategory(catId, "ToDelete"));
        SubCategory sub = await _context.SubCategories.FirstAsync(TestContext.Current.CancellationToken);

        var request = new SubCategoryRequest
        {
            Id = sub.Id,
            CategoryId = catId,
            Name = sub.Name,
            DisplayName = sub.DisplayName,
            Role = sub.Role,
            Order = sub.Order
        };

        await _sut.DeleteSubCategoryAsync(request);

        SubCategory? deleted = await _context.SubCategories.FirstOrDefaultAsync(sc => sc.Id == sub.Id, TestContext.Current.CancellationToken);
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
        List<SubCategory> all = await _context.SubCategories.ToListAsync(TestContext.Current.CancellationToken);

        var request = new SubCategoryRequest
        {
            Id = all[0].Id,
            CategoryId = all[0].CategoryId,
            Name = all[0].Name,
            DisplayName = all[0].DisplayName,
            Role = all[0].Role,
            Order = all[0].Order
        };

        await _sut.DeleteSubCategoryAsync(request);

        List<SubCategory> remaining = await _context.SubCategories.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
