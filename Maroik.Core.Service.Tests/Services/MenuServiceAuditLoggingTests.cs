using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Menus;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The navigation menu decides which pages each role may open (see AuthorizationFilter), so every change to
/// it — create, update, delete of a category or sub-category — is an audited administrative event.
/// </summary>
public class MenuServiceAuditLoggingTests : IAsyncDisposable
{
    /// <summary>The signed-in administrator performing the change (recorded in the audit log).</summary>
    private const string Actor = "admin@example.com";

    /// <summary>Fake <c>ICategoryRepository</c> injected into the system under test.</summary>
    private readonly FakeCategoryRepository _categoryRepo = new();
    /// <summary>Fake <c>ISubCategoryRepository</c> injected into the system under test.</summary>
    private readonly FakeSubCategoryRepository _subCategoryRepo = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<MenuService> _logger = new();

    /// <summary>
    /// Arranges a unit of work that always succeeds, and an empty locked menu (every update / delete locks the whole
    /// menu first; an empty one has no route to menu management, so the menu safeguard lets the change through).
    /// </summary>
    public MenuServiceAuditLoggingTests()
    {
        _unitOfWork.AnswerBeginAsync((_, _) => Task.CompletedTask);
        _unitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
        _categoryRepo.AnswerGetAllForUpdateAsync(_ => Task.FromResult<List<Category>>([]));
        _subCategoryRepo.AnswerGetAllForUpdateAsync(_ => Task.FromResult<List<SubCategory>>([]));
    }

    /// <summary>The service under test over the fake dependencies and the capturing logger.</summary>
    private MenuService CreateSut() => new(_categoryRepo, _subCategoryRepo, _unitOfWork, _logger);

    /// <summary>A persisted category (id 3).</summary>
    private static Category MakeCategory() => Category.Reconstitute(3, "Forum", "Forum", "/icons/forum.png", "Board", "Index", Role.User, 0L);

    /// <summary>A persisted sub-category (id 4) under category 3.</summary>
    private static SubCategory MakeSubCategory() => SubCategory.Reconstitute(4, 3, BoardTypes.FreeForum, BoardTypes.FreeForum, "/icons/forum.png", "Index", Role.User, 0L);

    /// <summary>Asserts exactly one Information entry was written, containing <paramref name="containing"/> and naming the acting admin.</summary>
    private void AssertOnlyInformation(string containing)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information);
        Assert.Contains(containing, record.Message, StringComparison.Ordinal);
        Assert.Contains($"by admin {Actor}", record.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that creating a menu category logs its name at Information.</summary>
    [Fact]
    public async Task CreateCategory_LogsInformation()
    {
        _categoryRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);

        await CreateSut().CreateCategoryAsync(new CategoryRequest { Name = "Forum", DisplayName = "Forum", IconPath = "/i.png", Controller = "Forum", Action = "Index", Role = Role.User }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu category Forum created");
    }

    /// <summary>Verifies that updating a menu category logs its ID and name at Information.</summary>
    [Fact]
    public async Task UpdateCategory_LogsInformationWithTheId()
    {
        _categoryRepo.AnswerFindByIdForUpdateAsync((_, _) => Task.FromResult<Category?>(MakeCategory()));
        _categoryRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        await CreateSut().UpdateCategoryAsync(new CategoryRequest { Id = 3, Name = "Forum", DisplayName = "Forum", IconPath = "/i.png", Controller = "Forum", Action = "Index", Role = Role.Admin }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu category 3 (Forum) updated");
    }

    /// <summary>Verifies that deleting a menu category logs its ID and name at Information.</summary>
    [Fact]
    public async Task DeleteCategory_LogsInformation()
    {
        _categoryRepo.AnswerDeleteByIdAsync((_, _) => Task.CompletedTask);

        await CreateSut().DeleteCategoryAsync(new CategoryRequest { Id = 3, Name = "Forum" }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu category 3 (Forum) deleted");
    }

    /// <summary>Verifies that creating a sub-category logs its name at Information.</summary>
    [Fact]
    public async Task CreateSubCategory_LogsInformation()
    {
        _subCategoryRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);

        await CreateSut().CreateSubCategoryAsync(new SubCategoryRequest { CategoryId = 3, Name = "FreeForum", DisplayName = "Free", IconPath = "/i.png", Action = "Index", Role = Role.User }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu sub-category FreeForum created");
    }

    /// <summary>Verifies that updating a sub-category logs its ID and name at Information.</summary>
    [Fact]
    public async Task UpdateSubCategory_LogsInformationWithTheId()
    {
        _subCategoryRepo.AnswerFindByIdForUpdateAsync((_, _) => Task.FromResult<SubCategory?>(MakeSubCategory()));
        _subCategoryRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        await CreateSut().UpdateSubCategoryAsync(new SubCategoryRequest { Id = 4, CategoryId = 3, Name = "FreeForum", DisplayName = "Free", IconPath = "/i.png", Action = "Index", Role = Role.Admin }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu sub-category 4 (FreeForum) updated");
    }

    /// <summary>Verifies that deleting a sub-category logs its ID and name at Information.</summary>
    [Fact]
    public async Task DeleteSubCategory_LogsInformation()
    {
        _subCategoryRepo.AnswerDeleteByIdAsync((_, _) => Task.CompletedTask);

        await CreateSut().DeleteSubCategoryAsync(new SubCategoryRequest { Id = 4, Name = "FreeForum" }, Actor, TestContext.Current.CancellationToken);

        AssertOnlyInformation("Menu sub-category 4 (FreeForum) deleted");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
