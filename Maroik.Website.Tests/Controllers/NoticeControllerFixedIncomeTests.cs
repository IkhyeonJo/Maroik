using Maroik.Core.Contract.Constants;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the FixedIncome action group of <c>NoticeController</c>
/// (all <c>[RequiredHttpPostAccess(Role = Role.User)]</c>-gated). Representative of the two
/// near-identical resource groups on this controller (FixedIncome/FixedExpenditure);
/// FixedExpenditure is not separately covered here.
///
/// <c>CreateFixedIncome</c>/<c>UpdateFixedIncome</c>'s DB-write paths always call
/// <c>AssetBalanceDomainService.GetAssetAsync</c>, which runs a raw SQL query with
/// <c>FOR UPDATE</c> row locking (<c>AssetRepository.FindByEmailAndProductNameForUpdateAsync</c>) —
/// required for safe concurrent balance updates against real PostgreSQL, but the EF Core
/// InMemory provider used by this test factory cannot execute raw SQL at all. The resulting
/// unhandled exception gets caught by <c>UseExceptionHandler("/Exception/Error")</c>, which
/// re-executes the request against that (GET-only) path while preserving the original POST
/// verb, surfacing to the client as an unrelated-looking 405 Method Not Allowed. This is a
/// provider limitation (confirmed by inspecting the repository's raw SQL directly), not
/// exercisable through this harness — it works against the real PostgreSQL provider. See
/// ManagementControllerAccountTests (HasDefaultValueSql) and AccountBookControllerAssetTests
/// (transactions) for the other two InMemory-provider gaps found in this codebase. Accordingly,
/// only the validation/not-found branches that never reach that call are covered here.
/// </summary>
[Collection("Website Integration")]
public class NoticeControllerFixedIncomeTests
{
    private const string Email = "notice-user@test.com";
    private const string AssetProductName = "notice-asset";

    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NoticeControllerFixedIncomeTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsync() =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, Email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    // -- CreateFixedIncome: role gating -----------------------------------------

    [Fact]
    public async Task CreateFixedIncome_AnonymousSession_IsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Notice/CreateFixedIncome");
        request.Content = System.Net.Http.Json.JsonContent.Create(new { });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- CreateFixedIncome: validation (fails before the asset lookup) -----------

    [Fact]
    public async Task CreateFixedIncome_InvalidDepositMonth_ReturnsInputInvalid()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 0, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Salary",
            Amount = 100, DepositMonth = 13, DepositDay = 1, MaturityDate = "2030-12-31",
            Note = "", DepositMyAssetProductName = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/CreateFixedIncome", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Input is invalid", json);
    }

    [Fact]
    public async Task CreateFixedIncome_InvalidDepositDayForMonth_ReturnsInputInvalid()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 0, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Salary",
            Amount = 100, DepositMonth = 2, DepositDay = 30, MaturityDate = "2030-12-31",
            Note = "", DepositMyAssetProductName = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/CreateFixedIncome", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Input is invalid", json);
    }

    // -- IsFixedIncomeExists / UpdateFixedIncome: not-found branches -------------
    // (GetByIdAsync / GetByAccountEmailAsync use ordinary LINQ, not the raw-SQL path.)

    [Fact]
    public async Task IsFixedIncomeExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/Notice/IsFixedIncomeExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task UpdateFixedIncome_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 999999, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Salary",
            Amount = 100, DepositMonth = 6, DepositDay = 1, MaturityDate = "2030-12-31",
            Note = "", DepositMyAssetProductName = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/UpdateFixedIncome", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }
}
