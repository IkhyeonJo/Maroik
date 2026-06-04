using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="MenuService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers navigation category and sub-category CRUD operations.
/// </summary>
public class MenuServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<ISubCategoryRepository> _subCategoryRepo = new();

    private MenuService CreateSut() => new(
        _categoryRepo.Object,
        _subCategoryRepo.Object,
        NullLogger<MenuService>.Instance);

    // -- Helpers --------------------------------------------------------------

    private static Category MakeCategory(long id = 1, string name = "Forum") =>
        Category.Reconstitute(id, name, name, "/icons/forum.png", "Board", "Index", Role.User, 0L);

    private static SubCategory MakeSubCategory(long id = 1, string name = nameof(BoardType.FreeForum), long categoryId = 1) =>
        SubCategory.Reconstitute(id, categoryId, name, name, "/icons/forum.png", "Index", Role.User, 0L);

    // -- GetAllCategoriesAsync ------------------------------------------------

    /// <summary>Verifies that <c>GetAllCategoriesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAllCategoriesAsync_DelegatesToRepository()
    {
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeCategory()]);
        var sut = CreateSut();

        IEnumerable<CategoryResponse> result = await sut.GetAllCategoriesAsync();

        Assert.Single(result);
    }

    // -- GetAllSubCategoriesAsync ---------------------------------------------

    /// <summary>Verifies that <c>GetAllSubCategoriesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAllSubCategoriesAsync_DelegatesToRepository()
    {
        _subCategoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeSubCategory()]);
        var sut = CreateSut();

        IEnumerable<SubCategoryResponse> result = await sut.GetAllSubCategoriesAsync();

        Assert.Single(result);
    }

    // -- GetCategoryByIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetCategoryByIdAsync</c> returns category when found.</summary>
    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsCategory_WhenFound()
    {
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeCategory(id: 5, name: "Board")]);
        var sut = CreateSut();

        CategoryResponse? result = await sut.GetCategoryByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("Board", result.Name);
    }

    /// <summary>Verifies that <c>GetCategoryByIdAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeCategory(id: 1)]);
        var sut = CreateSut();

        CategoryResponse? result = await sut.GetCategoryByIdAsync(999);

        Assert.Null(result);
    }

    // -- GetSubCategoryByIdAsync ----------------------------------------------

    /// <summary>Verifies that <c>GetSubCategoryByIdAsync</c> returns sub category when found.</summary>
    [Fact]
    public async Task GetSubCategoryByIdAsync_ReturnsSubCategory_WhenFound()
    {
        _subCategoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeSubCategory(id: 3, name: nameof(BoardType.PrivateNote))]);
        var sut = CreateSut();

        SubCategoryResponse? result = await sut.GetSubCategoryByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    /// <summary>Verifies that <c>GetSubCategoryByIdAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetSubCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        _subCategoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeSubCategory(id: 1)]);
        var sut = CreateSut();

        SubCategoryResponse? result = await sut.GetSubCategoryByIdAsync(999);

        Assert.Null(result);
    }

    // -- CreateCategoryAsync --------------------------------------------------

    /// <summary>Verifies that <c>CreateCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task CreateCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCategoryAsync(new CategoryRequest { Name = "NewCategory" });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task CreateCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCategoryAsync(new CategoryRequest { Name = "Bad" });

        Assert.False(result.Success);
    }

    // -- UpdateCategoryAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdateCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task UpdateCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCategoryAsync(new CategoryRequest { Id = 1, Name = "Updated" });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>UpdateCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task UpdateCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // -- DeleteCategoryAsync --------------------------------------------------

    /// <summary>Verifies that <c>DeleteCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _categoryRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _categoryRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCategoryAsync(new CategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // -- CreateSubCategoryAsync -----------------------------------------------

    /// <summary>Verifies that <c>CreateSubCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task CreateSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.CreateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateSubCategoryAsync(new SubCategoryRequest { Name = "NewSub" });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateSubCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task CreateSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.CreateAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateSubCategoryAsync(new SubCategoryRequest { Name = "Bad" });

        Assert.False(result.Success);
    }

    // -- UpdateSubCategoryAsync -----------------------------------------------

    /// <summary>Verifies that <c>UpdateSubCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task UpdateSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateSubCategoryAsync(new SubCategoryRequest { Id = 1, Name = "Updated" });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>UpdateSubCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task UpdateSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<SubCategory>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }

    // -- DeleteSubCategoryAsync -----------------------------------------------

    /// <summary>Verifies that <c>DeleteSubCategoryAsync</c> returns success when repository succeeds.</summary>
    [Fact]
    public async Task DeleteSubCategoryAsync_ReturnsSuccess_WhenRepositorySucceeds()
    {
        _subCategoryRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>DeleteSubCategoryAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task DeleteSubCategoryAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _subCategoryRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteSubCategoryAsync(new SubCategoryRequest { Id = 1 });

        Assert.False(result.Success);
    }
}
