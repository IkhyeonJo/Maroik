using Maroik.Core.Contract.Constants;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Income and Expenditure action groups of
/// <c>AccountBookController</c> (all <c>[RequiredHttpPostAccess(Role = Role.User)]</c>-gated).
///
/// Unlike Asset, <c>IncomeService</c>/<c>ExpenditureService</c> call
/// <c>_unitOfWork.BeginAsync</c> as the very first statement in Create/Update/Delete — before
/// their own try/catch even starts — so under the EF Core InMemory provider (which cannot
/// execute transactions) that throws immediately, and neither the service's catch block nor
/// the controller (no try/catch on these three actions) can intercept it. The exception
/// reaches <c>UseExceptionHandler("/Exception/Error")</c>, which re-executes the pipeline
/// against that GET-only path while preserving the original POST verb, surfacing as an
/// unrelated-looking 405 (same root cause as documented in
/// <c>NoticeControllerFixedIncomeTests</c>). So Create/Update/Delete are only exercised here up
/// to the point of ModelState validation (which runs, and returns cleanly, before the service is
/// ever called); IsIncomeExists/IsExpenditureExists/GetAmountLabel use plain reads with no
/// transaction and are fully covered.
/// </summary>
[Collection("Website Integration")]
public class AccountBookControllerIncomeExpenditureTests
{
    private const string Email = "accountbook-incexp-user@test.com";

    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountBookControllerIncomeExpenditureTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsync() =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, Email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private void SeedAssetIfMissing(ApplicationDbContext db)
    {
        if (db.Assets.Any(a => a.AccountEmail == Email && a.ProductName == "some-asset")) return;
        db.Assets.Add(new Asset
        {
            ProductName = "some-asset",
            AccountEmail = Email,
            Item = "CashAsset",
            Amount = 1000,
            MonetaryUnit = "KRW",
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private long SeedIncome()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SeedAssetIfMissing(db);
        var income = new Income
        {
            AccountEmail = Email,
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary",
            Amount = 1000,
            DepositMyAssetProductName = "some-asset",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.Incomes.Add(income);
        db.SaveChanges();
        return income.Id;
    }

    private long SeedExpenditure()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SeedAssetIfMissing(db);
        var expenditure = new Expenditure
        {
            AccountEmail = Email,
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Lunch",
            Amount = 50,
            PaymentMethod = "some-asset",
            MyDepositAsset = "some-asset",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.Expenditures.Add(expenditure);
        db.SaveChanges();
        return expenditure.Id;
    }

    // -- CreateIncome: role gating + ModelState validation ------------------------

    [Fact]
    public async Task CreateIncome_AdminSession_IsForbidden()
    {
        var session = await AuthenticatedSessionHelper.LoginAsync(_factory, _client, "accountbook-incexp-admin@test.com", "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateIncome", new { Id = 0, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "x", Amount = 1, DepositMyAssetProductName = "a", Note = "" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateIncome_MissingMainClass_FailsModelStateCleanly()
    {
        var session = await LoginAsync();
        var payload = new { Id = 0, MainClass = (string?)null, SubClass = "LaborIncome", Content = "x", Amount = 1, DepositMyAssetProductName = "a", Note = "" };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateIncome", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json);
    }

    // -- IsIncomeExists -------------------------------------------------------------

    [Fact]
    public async Task IsIncomeExists_SeededRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedIncome();

        using var request = session.BuildJsonPostRequest($"/AccountBook/IsIncomeExists?id={id}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    [Fact]
    public async Task IsIncomeExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/IsIncomeExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task GetIncomeAmountLabel_UnknownProduct_ReturnsBaseLabel()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/GetIncomeAmountLabel?productName=no-such-asset");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    // -- CreateExpenditure: role gating + ModelState validation --------------------

    [Fact]
    public async Task CreateExpenditure_MissingPaymentMethod_FailsModelStateCleanly()
    {
        var session = await LoginAsync();
        var payload = new { Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "x", Amount = 1, PaymentMethod = (string?)null, MyDepositAsset = "a", Note = "" };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateExpenditure", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json);
    }

    // -- IsExpenditureExists ----------------------------------------------------------

    [Fact]
    public async Task IsExpenditureExists_SeededRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedExpenditure();

        using var request = session.BuildJsonPostRequest($"/AccountBook/IsExpenditureExists?id={id}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    [Fact]
    public async Task IsExpenditureExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/IsExpenditureExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task GetExpenditureAmountLabel_UnknownProduct_ReturnsBaseLabel()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/GetExpenditureAmountLabel?productName=no-such-asset");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }
}
