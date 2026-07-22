using Maroik.Core.Contract.Constants;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Asset CRUD action group of <c>AccountBookController</c>
/// (all <c>[RequiredHttpPostAccess(Role = Role.User)]</c>-gated). Representative of the
/// three near-identical resource groups on this controller (Asset/Income/Expenditure);
/// Income and Expenditure are not separately covered here.
/// </summary>
[Collection("Website Integration")]
public class AccountBookControllerAssetTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountBookControllerAssetTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "accountbook-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private Task<AuthenticatedSession> LoginAsAdminAsync(string email = "accountbook-admin@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);

    private void SeedAsset(string accountEmail, string productName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Assets.Any(a => a.AccountEmail == accountEmail && a.ProductName == productName)) return;
        db.Assets.Add(new Asset
        {
            ProductName = productName,
            AccountEmail = accountEmail,
            Item = "CashAsset",
            Amount = 1000,
            MonetaryUnit = "KRW",
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static object ValidAssetPayload(string productName) => new
    {
        ProductName = productName,
        Item = "CashAsset",
        Amount = 5000,
        MonetaryUnit = "KRW",
        Note = "",
        Deleted = false
    };

    // -- CreateAsset: role gating -----------------------------------------------

    [Fact]
    public async Task CreateAsset_AdminSession_IsForbidden()
    {
        var session = await LoginAsAdminAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateAsset", ValidAssetPayload("blocked-asset"));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- CreateAsset: validation --------------------------------------------------

    [Fact]
    public async Task CreateAsset_MissingProductName_FailsModelState()
    {
        var session = await LoginAsUserAsync();
        var payload = new { ProductName = (string?)null, Item = "CashAsset", Amount = 100m, MonetaryUnit = "KRW", Note = "", Deleted = false };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateAsset", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task CreateAsset_UnrecognizedItem_ReturnsInputInvalid()
    {
        var session = await LoginAsUserAsync();
        var payload = new { ProductName = "BadItemAsset", Item = "NotARealItemType", Amount = 100m, MonetaryUnit = "KRW", Note = "", Deleted = false };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateAsset", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Input is invalid", json);
    }

    // -- CreateAsset / IsAssetExists: success path --------------------------------

    [Fact]
    public async Task CreateAsset_ThenIsAssetExists_FindsTheNewAsset()
    {
        var session = await LoginAsUserAsync();
        const string productName = "created-by-test-asset";
        using var createRequest = session.BuildJsonPostRequest("/AccountBook/CreateAsset", ValidAssetPayload(productName));
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"result\":true", createJson);

        using var existsRequest = session.BuildJsonPostRequest($"/AccountBook/IsAssetExists?productName={productName}");
        var existsResponse = await _client.SendAsync(existsRequest, TestContext.Current.CancellationToken);
        string existsJson = await existsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", existsJson);
    }

    [Fact]
    public async Task CreateAsset_DuplicateProductName_ReturnsAlreadyExistsError()
    {
        var session = await LoginAsUserAsync();
        const string productName = "duplicate-asset";
        using var firstRequest = session.BuildJsonPostRequest("/AccountBook/CreateAsset", ValidAssetPayload(productName));
        await _client.SendAsync(firstRequest, TestContext.Current.CancellationToken);

        using var secondRequest = session.BuildJsonPostRequest("/AccountBook/CreateAsset", ValidAssetPayload(productName));
        var response = await _client.SendAsync(secondRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("The asset already exists.", json);
    }

    [Fact]
    public async Task IsAssetExists_UnknownProductName_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/IsAssetExists?productName=nobody-asset-xyz");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    // -- UpdateAsset ---------------------------------------------------------------
    //
    // AssetService.UpdateAsync wraps its entire body in an IUnitOfWork transaction
    // (BeginAsync/CommitAsync/RollbackAsync) because renaming an asset cascades updates to
    // Income/Expenditure records atomically. The EF Core InMemory provider used by this test
    // factory does not support transactions at all ("Transactions are not supported by the
    // in-memory store."), so every call — success or not-found — throws before UpdateAsset's
    // own logic runs. This is a provider limitation, not exercisable through this harness; it
    // works against the real PostgreSQL provider. See ManagementControllerAccountTests for the
    // analogous HasDefaultValueSql limitation on CreateAccount.

    // -- DeleteAsset ---------------------------------------------------------------

    [Fact]
    public async Task DeleteAsset_ExistingAsset_ReturnsSuccessResult()
    {
        var session = await LoginAsUserAsync();
        const string productName = "to-delete-asset";
        SeedAsset("accountbook-user@test.com", productName);

        using var request = session.BuildJsonPostRequest("/AccountBook/DeleteAsset", new { ProductName = productName });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }
}
