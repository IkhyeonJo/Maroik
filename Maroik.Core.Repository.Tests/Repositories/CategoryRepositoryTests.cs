using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CategoryRepository"/> using an EF Core InMemory database.
/// </summary>
public class CategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryRepository _sut;

    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CategoryRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Category MakeCategory(string name, string role = "User", long order = 1) => new()
    {
        Name = name,
        DisplayName = name,
        IconPath = "/icons/icon.svg",
        Controller = name,
        Action = "Index",
        Role = role,
        Order = order
    };

    private async Task SeedAsync(params Category[] categories)
    {
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
    }

    // ── GetCategoryAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryAsync_ReturnsAllCategories()
    {
        await SeedAsync(
            MakeCategory("Dashboard", order: 1),
            MakeCategory("Finance", order: 2));

        IEnumerable<CategoryResponse> result = await _sut.GetCategoryAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsEmpty_WhenNoCategories()
    {
        IEnumerable<CategoryResponse> result = await _sut.GetCategoryAsync();

        Assert.Empty(result);
    }

    // ── GetCategoryByRoleAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryByRoleAsync_ReturnsOnlyCategoriesForRole()
    {
        await SeedAsync(
            MakeCategory("Dashboard", role: "User", order: 1),
            MakeCategory("Admin", role: "Admin", order: 2),
            MakeCategory("Finance", role: "User", order: 3));

        IEnumerable<CategoryResponse> result = await _sut.GetCategoryByRoleAsync("User");

        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal("User", c.Role));
    }

    [Fact]
    public async Task GetCategoryByRoleAsync_ReturnsOrderedByOrder()
    {
        await SeedAsync(
            MakeCategory("Finance", role: "User", order: 2),
            MakeCategory("Dashboard", role: "User", order: 1));

        IEnumerable<CategoryResponse> result = await _sut.GetCategoryByRoleAsync("User");
        List<CategoryResponse> list = result.ToList();

        Assert.Equal("Dashboard", list[0].Name);
        Assert.Equal("Finance", list[1].Name);
    }

    // ── CreateCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategoryAsync_AddsCategoryToDatabase()
    {
        var request = new CategoryRequest
        {
            Name = "NewMenu",
            DisplayName = "New Menu",
            IconPath = "/icons/new.svg",
            Controller = "NewMenu",
            Action = "Index",
            Role = "User",
            Order = 5
        };

        await _sut.CreateCategoryAsync(request);

        Category? saved = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "NewMenu");
        Assert.NotNull(saved);
        Assert.Equal("User", saved.Role);
        Assert.Equal(5, saved.Order);
    }

    // ── UpdateCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesExistingCategory()
    {
        await SeedAsync(MakeCategory("OldName", order: 1));
        Category category = await _context.Categories.FirstAsync();

        var request = new CategoryRequest
        {
            Id = category.Id,
            Name = "UpdatedName",
            DisplayName = "Updated Display",
            IconPath = "/icons/updated.svg",
            Controller = "UpdatedName",
            Action = "Index",
            Role = "Admin",
            Order = 10
        };

        await _sut.UpdateCategoryAsync(request);

        Category? updated = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedName", updated.Name);
        Assert.Equal("Admin", updated.Role);
        Assert.Equal(10, updated.Order);
    }

    // ── DeleteCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategoryFromDatabase()
    {
        await SeedAsync(MakeCategory("ToDelete"));
        Category category = await _context.Categories.FirstAsync();

        var request = new CategoryRequest
        {
            Id = category.Id,
            Name = "ToDelete",
            DisplayName = "ToDelete",
            Role = "User",
            Order = 1
        };

        await _sut.DeleteCategoryAsync(request);

        Category? deleted = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteCategoryAsync_DoesNotAffectOtherCategories()
    {
        await SeedAsync(MakeCategory("Cat1", order: 1), MakeCategory("Cat2", order: 2));
        List<Category> all = await _context.Categories.ToListAsync();

        var request = new CategoryRequest
        {
            Id = all[0].Id,
            Name = all[0].Name,
            DisplayName = all[0].DisplayName,
            Role = all[0].Role,
            Order = all[0].Order
        };

        await _sut.DeleteCategoryAsync(request);

        List<Category> remaining = await _context.Categories.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
