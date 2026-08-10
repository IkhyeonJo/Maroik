using Maroik.Core.Contract.Misc.Constants;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Menu (Category/SubCategory) CRUD action group of
/// <c>ManagementController</c> — previously entirely uncovered beyond a GET smoke test in
/// <c>ManagementControllerTests.cs</c>. Along the way this uncovered a real bug in
/// <c>MenuService.UpdateCategoryAsync</c>/<c>UpdateSubCategoryAsync</c>: both built the updated
/// entity via <c>Reconstitute</c> and called the repository's generic <c>UpdateEntityAsync</c>
/// directly, which silently no-ops (returns without throwing) when the given Id doesn't match
/// any row — so updating a nonexistent category/sub-category used to report
/// "successfully updated" while changing nothing. Fixed by checking existence first, matching
/// the fetch-then-act pattern already used elsewhere (e.g. <c>AccountService.UpdateAccountAsync</c>).
/// </summary>
[Collection("Website Integration")]
public class ManagementControllerMenuTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagementControllerMenuTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsAdminAsync(string email = "management-menu-admin@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "management-menu-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private static string UniqueName([System.Runtime.CompilerServices.CallerMemberName] string testName = "") =>
        $"{testName}-{Guid.NewGuid():N}";

    private static object ValidCategoryPayload(string name, int id = 0) => new
    {
        Id = id,
        Name = name,
        DisplayName = name,
        IconPath = "/icons/test.png",
        Controller = "Notice",
        Action = "",
        Role = Role.User,
        Order = 500
    };

    private static object ValidSubCategoryPayload(string name, long categoryId, int id = 0) => new
    {
        Id = id,
        CategoryId = categoryId,
        Name = name,
        DisplayName = name,
        IconPath = "/icons/test.png",
        Controller = "",
        Action = "Index",
        Role = Role.User,
        Order = 500
    };

    // -- CreateCategory: role gating --------------------------------------------

    [Fact]
    public async Task CreateCategory_UserSession_IsForbidden()
    {
        var user = await LoginAsUserAsync();
        using var request = user.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(UniqueName()));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- CreateCategory / IsCategoryExists: success path ------------------------

    [Fact]
    public async Task CreateCategory_ThenIsCategoryExists_FindsTheNewCategory()
    {
        var admin = await LoginAsAdminAsync();
        string name = UniqueName();
        using var createRequest = admin.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(name));
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"result\":true", createJson);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var created = db.Categories.Single(c => c.Name == name);

        using var existsRequest = admin.BuildJsonPostRequest($"/Management/IsCategoryExists?id={created.Id}");
        var existsResponse = await _client.SendAsync(existsRequest, TestContext.Current.CancellationToken);
        string existsJson = await existsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", existsJson);
    }

    [Fact]
    public async Task IsCategoryExists_UnknownId_ReturnsFailureResult()
    {
        var admin = await LoginAsAdminAsync();
        using var request = admin.BuildJsonPostRequest("/Management/IsCategoryExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    // -- UpdateCategory ----------------------------------------------------------

    [Fact]
    public async Task UpdateCategory_ExistingCategory_PersistsChanges()
    {
        var admin = await LoginAsAdminAsync();
        string name = UniqueName();
        using var createRequest = admin.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(name));
        await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var created = db.Categories.Single(c => c.Name == name);

        string updatedName = UniqueName();
        using var updateRequest = admin.BuildJsonPostRequest("/Management/UpdateCategory", ValidCategoryPayload(updatedName, (int)created.Id));
        var response = await _client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(updatedName, verifyDb.Categories.Single(c => c.Id == created.Id).Name);
    }

    /// <summary>
    /// Regression test for the "silently no-ops but reports success" bug found and fixed in
    /// <c>MenuService.UpdateCategoryAsync</c> — see this file's class-level doc comment.
    /// </summary>
    [Fact]
    public async Task UpdateCategory_UnknownId_ReturnsFailureResult()
    {
        var admin = await LoginAsAdminAsync();
        using var request = admin.BuildJsonPostRequest("/Management/UpdateCategory", ValidCategoryPayload("Ghost", 999999));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    // -- DeleteCategory ------------------------------------------------------------

    [Fact]
    public async Task DeleteCategory_ExistingCategory_RemovesIt()
    {
        var admin = await LoginAsAdminAsync();
        string name = UniqueName();
        using var createRequest = admin.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(name));
        await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var created = db.Categories.Single(c => c.Name == name);

        using var deleteRequest = admin.BuildJsonPostRequest("/Management/DeleteCategory", ValidCategoryPayload(name, (int)created.Id));
        var response = await _client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(verifyDb.Categories.Any(c => c.Id == created.Id));
    }

    // -- CreateSubCategory / IsSubCategoryExists: success path -------------------

    [Fact]
    public async Task CreateSubCategory_ThenIsSubCategoryExists_FindsTheNewSubCategory()
    {
        var admin = await LoginAsAdminAsync();
        string catName = UniqueName("cat");
        using var createCatRequest = admin.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(catName));
        await _client.SendAsync(createCatRequest, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = db.Categories.Single(c => c.Name == catName);

        string subName = UniqueName();
        using var createSubRequest = admin.BuildJsonPostRequest("/Management/CreateSubCategory", ValidSubCategoryPayload(subName, category.Id));
        var createResponse = await _client.SendAsync(createSubRequest, TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"result\":true", createJson);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdSub = verifyDb.SubCategories.Single(s => s.Name == subName);

        using var existsRequest = admin.BuildJsonPostRequest($"/Management/IsSubCategoryExists?id={createdSub.Id}");
        var existsResponse = await _client.SendAsync(existsRequest, TestContext.Current.CancellationToken);
        string existsJson = await existsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", existsJson);
    }

    [Fact]
    public async Task IsSubCategoryExists_UnknownId_ReturnsFailureResult()
    {
        var admin = await LoginAsAdminAsync();
        using var request = admin.BuildJsonPostRequest("/Management/IsSubCategoryExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    // -- UpdateSubCategory ----------------------------------------------------------

    [Fact]
    public async Task UpdateSubCategory_UnknownId_ReturnsFailureResult()
    {
        var admin = await LoginAsAdminAsync();
        using var request = admin.BuildJsonPostRequest("/Management/UpdateSubCategory", ValidSubCategoryPayload("Ghost", categoryId: 1, id: 999999));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    // -- DeleteSubCategory ------------------------------------------------------------

    [Fact]
    public async Task DeleteSubCategory_ExistingSubCategory_RemovesIt()
    {
        var admin = await LoginAsAdminAsync();
        string catName = UniqueName("cat");
        using var createCatRequest = admin.BuildJsonPostRequest("/Management/CreateCategory", ValidCategoryPayload(catName));
        await _client.SendAsync(createCatRequest, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = db.Categories.Single(c => c.Name == catName);

        string subName = UniqueName();
        using var createSubRequest = admin.BuildJsonPostRequest("/Management/CreateSubCategory", ValidSubCategoryPayload(subName, category.Id));
        await _client.SendAsync(createSubRequest, TestContext.Current.CancellationToken);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdSub = verifyDb.SubCategories.Single(s => s.Name == subName);

        using var deleteRequest = admin.BuildJsonPostRequest("/Management/DeleteSubCategory", ValidSubCategoryPayload(subName, category.Id, (int)createdSub.Id));
        var response = await _client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);

        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(finalDb.SubCategories.Any(s => s.Id == createdSub.Id));
    }
}
