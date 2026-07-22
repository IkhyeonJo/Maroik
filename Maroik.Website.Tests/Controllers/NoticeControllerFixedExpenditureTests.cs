using Maroik.Core.Contract.Constants;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the FixedExpenditure action group of <c>NoticeController</c>
/// (all <c>[RequiredHttpPostAccess(Role = Role.User)]</c>-gated). Mirrors
/// <c>NoticeControllerFixedIncomeTests</c> — see that file for the full explanation of why
/// <c>CreateFixedExpenditure</c>/<c>UpdateFixedExpenditure</c>'s DB-write paths (which always
/// call <c>AssetBalanceDomainService.GetAssetAsync</c>, a raw-SQL <c>FOR UPDATE</c> query) are
/// not exercisable under the EF Core InMemory provider used by this test factory. Only the
/// validation/not-found branches that never reach that call are covered here.
/// </summary>
[Collection("Website Integration")]
public class NoticeControllerFixedExpenditureTests
{
    private const string Email = "notice-expenditure-user@test.com";
    private const string AssetProductName = "notice-expenditure-asset";

    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NoticeControllerFixedExpenditureTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsync() =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, Email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    // -- CreateFixedExpenditure: role gating ------------------------------------

    [Fact]
    public async Task CreateFixedExpenditure_AnonymousSession_IsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Notice/CreateFixedExpenditure");
        request.Content = System.Net.Http.Json.JsonContent.Create(new { });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- CreateFixedExpenditure: validation (fails before the asset lookup) -----

    [Fact]
    public async Task CreateFixedExpenditure_InvalidDepositMonth_ReturnsInputInvalid()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Rent",
            Amount = 100, DepositMonth = 0, DepositDay = 1, MaturityDate = "2030-12-31",
            Note = "", PaymentMethod = AssetProductName, MyDepositAsset = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/CreateFixedExpenditure", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Input is invalid", json);
    }

    [Fact]
    public async Task CreateFixedExpenditure_InvalidDepositDayForMonth_ReturnsInputInvalid()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Rent",
            Amount = 100, DepositMonth = 4, DepositDay = 31, MaturityDate = "2030-12-31",
            Note = "", PaymentMethod = AssetProductName, MyDepositAsset = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/CreateFixedExpenditure", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Input is invalid", json);
    }

    // -- IsFixedExpenditureExists / UpdateFixedExpenditure: not-found branches ---

    [Fact]
    public async Task IsFixedExpenditureExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/Notice/IsFixedExpenditureExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task UpdateFixedExpenditure_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        var payload = new
        {
            Id = 999999, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Rent",
            Amount = 100, DepositMonth = 6, DepositDay = 1, MaturityDate = "2030-12-31",
            Note = "", PaymentMethod = AssetProductName, MyDepositAsset = AssetProductName, Unpunctuality = false
        };
        using var request = session.BuildJsonPostRequest("/Notice/UpdateFixedExpenditure", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }
}
