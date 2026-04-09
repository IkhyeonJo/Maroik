using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

public class MenuServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<ISubCategoryRepository> _subCategoryRepo = new();

    private MenuService CreateSut() => new(
        _categoryRepo.Object,
        _subCategoryRepo.Object,
        NullLogger<MenuService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CategoryResponse MakeCategory(int id = 1, string name = "Forum") => new()
    {
        Id = id,
        Name = name,
        DisplayName = name,
        Controller = "Board",
        IconPath = "/icons/forum.png",
        Role = "User"
    };

    private static SubCategoryResponse MakeSubCategory(int id = 1, string name = "FreeForum", int categoryId = 1) => new()
    {
        Id = id,
        Name = name,
        DisplayName = name,
        CategoryId = categoryId,
        Action = "Index",
        IconPath = "/icons/forum.png",
        Role = "User"
    };

    // ── GetAllCategoriesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCategoriesAsync_DelegatesToRepository()
    {
        _categoryRepo.Setup(r => r.GetCategoryAsync()).ReturnsAsync([MakeCategory()]);
        var sut = CreateSut();

        IEnumerable<CategoryResponse> result = await sut.GetAllCategoriesAsync();

        Assert.Single(result);
    }

    // ── GetAllSubCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllSubCategoriesAsync_DelegatesToRepository()
    {
        _subCategoryRepo.Setup(r => r.GetSubCategoryAsync()).ReturnsAsync([MakeSubCategory()]);
        var sut = CreateSut();

        IEnumerable<SubCategoryResponse> result = await sut.GetAllSubCategoriesAsync();

        Assert.Single(result);
    }

    // ── GetCategoryByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsCategory_WhenFound()
    {
        _categoryRepo.Setup(r => r.GetCategoryAsync()).ReturnsAsync([MakeCategory(id: 5, name: "Board")]);
        var sut = CreateSut();

        CategoryResponse? result = await sut.GetCategoryByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("Board", result.Name);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        _categoryRepo.Setup(r => r.GetCategoryAsync()).ReturnsAsync([MakeCategory(id: 1)]);
        var sut = CreateSut();

        CategoryResponse? result = await sut.GetCategoryByIdAsync(999);

        Assert.Null(result);
    }

    // ── GetSubCategoryByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetSubCategoryByIdAsync_ReturnsSubCategory_WhenFound()
    {
        _subCategoryRepo.Setup(r => r.GetSubCategoryAsync()).ReturnsAsync([MakeSubCategory(id: 3, name: "PrivateNote")]);
        var sut = CreateSut();

        SubCategoryResponse? result = await sut.GetSubCategoryByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetSubCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        _subCategoryRepo.Setup(r => r.GetSubCategoryAsync()).ReturnsAsync([MakeSubCategory(id: 1)]);
        var sut = CreateSut();

        SubCategoryResponse? result = await sut.GetSubCategoryByIdAsync(999);

        Assert.Null(result);
    }

    // ── CreateCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.CreateCategoryAsync(It.IsAny<CategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCategoryAsync(new CategoryRequest { Name = "NewCategory" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.CreateCategoryAsync(It.IsAny<CategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCategoryAsync(new CategoryRequest { Name = "Bad" });

        Assert.False(result.Success);
    }

    // ── UpdateCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.UpdateCategoryAsync(It.IsAny<CategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCategoryAsync(new CategoryRequest { Id = 1, Name = "Updated" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.UpdateCategoryAsync(It.IsAny<CategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // ── DeleteCategoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.DeleteCategoryAsync(It.IsAny<CategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.DeleteCategoryAsync(It.IsAny<CategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // ── CreateSubCategoryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.CreateSubCategoryAsync(It.IsAny<SubCategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateSubCategoryAsync(new SubCategoryRequest { Name = "NewSub" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.CreateSubCategoryAsync(It.IsAny<SubCategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateSubCategoryAsync(new SubCategoryRequest { Name = "Bad" });

        Assert.False(result.Success);
    }

    // ── UpdateSubCategoryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.UpdateSubCategoryAsync(It.IsAny<SubCategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateSubCategoryAsync(new SubCategoryRequest { Id = 1, Name = "Updated" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.UpdateSubCategoryAsync(It.IsAny<SubCategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // ── DeleteSubCategoryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.DeleteSubCategoryAsync(It.IsAny<SubCategoryRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.DeleteSubCategoryAsync(It.IsAny<SubCategoryRequest>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }
}
