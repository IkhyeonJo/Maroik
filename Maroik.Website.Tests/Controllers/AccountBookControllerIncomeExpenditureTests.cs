using Maroik.Core.Domain.Primitives;
using Maroik.Core.PostgreSQL.Contexts;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Website.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Income and Expenditure action groups of
/// <c>AccountBookController</c> (all <c>[RequiredHttpPostAccess(Role = Role.User)]</c>-gated).
/// Runs against a real PostgreSQL instance (via <see cref="MaroikWebApplicationFactory"/>'s
/// Testcontainers setup, schema loaded from the real production init script), so
/// <c>IncomeService</c>/<c>ExpenditureService</c>'s <c>IUnitOfWork</c> transactions are
/// exercised for real, including their Asset-balance side effects.
/// </summary>
[Collection("Website Integration")]
public class AccountBookControllerIncomeExpenditureTests(MaroikWebApplicationFactory factory)
{
    /// <summary>The account every test in this class signs in as.</summary>
    private const string Email = "accountbook-incexp-user@test.com";
    /// <summary>Default asset the seeded incomes/expenditures use.</summary>
    private const string AssetProductName = "some-asset";

    /// <summary>Client for the shared test host; redirects are not followed so they can be asserted.</summary>
    private readonly HttpClient _client = factory.CreateTestClient(followRedirects: false);
    
    /// <summary>Seeds (if missing) the User account <see cref="Email"/> and signs in as it.</summary>
    private Task<AuthenticatedSession> LoginAsync() =>
        AuthenticatedSessionHelper.LoginAsync(factory, _client, Email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    /// <summary>Adds (without saving) an asset of <see cref="Email"/> named <paramref name="productName"/>, unless it already exists.</summary>
    private static void SeedAssetIfMissing(ApplicationDbContext db, string productName = AssetProductName)
    {
        if (db.Assets.Any(a => a.AccountEmail == Email && a.ProductName == productName)) return;
        db.Assets.Add(new Asset
        {
            ProductName = productName,
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

    /// <summary>The stored balance of the asset named <paramref name="productName"/>.</summary>
    private decimal GetAssetBalance(string productName = AssetProductName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return db.Assets.Single(a => a.AccountEmail == Email && a.ProductName == productName).Amount;
    }

    /// <summary>A unique product name per call, so balance-delta assertions in a single test
    /// can't be perturbed by other tests in this class concurrently touching the shared asset.</summary>
    private static string UniqueAssetName([System.Runtime.CompilerServices.CallerMemberName] string testName = "") =>
        $"{testName}-{Guid.NewGuid():N}";

    /// <summary>Inserts an income into <paramref name="productName"/> directly into the database (the asset balance is not touched) and returns its id.</summary>
    private long SeedIncome(decimal amount = 1000, string productName = AssetProductName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SeedAssetIfMissing(db, productName);
        var income = new Income
        {
            AccountEmail = Email,
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary",
            Amount = amount,
            DepositMyAssetProductName = productName,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.Incomes.Add(income);
        db.SaveChanges();
        return income.Id;
    }

    /// <summary>Inserts an expenditure paid from <paramref name="productName"/> directly into the database (the asset balance is not touched) and returns its id.</summary>
    private long SeedExpenditure(decimal amount = 50, string productName = AssetProductName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SeedAssetIfMissing(db, productName);
        var expenditure = new Expenditure
        {
            AccountEmail = Email,
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Lunch",
            Amount = amount,
            PaymentMethod = productName,
            // Left empty (not equal to PaymentMethod): MyDepositAsset only has meaning for
            // "transfer"-classified expenditures (moving money between two assets). Setting it
            // to the same value as PaymentMethod — which the real CreateExpenditure flow never
            // does for a non-transfer category like this one — would make
            // AdjustAssetBalancesAsync deposit and withdraw the *same* asset on revert, net-zero.
            MyDepositAsset = null,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.Expenditures.Add(expenditure);
        db.SaveChanges();
        return expenditure.Id;
    }

    // -- CreateIncome: role gating + ModelState validation ------------------------

    /// <summary>Create income admin session is forbidden.</summary>
    [Fact]
    public async Task CreateIncome_AdminSession_IsForbidden()
    {
        var session = await AuthenticatedSessionHelper.LoginAsync(factory, _client, "accountbook-incexp-admin@test.com", "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateIncome", new { Id = 0, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "x", Amount = "1", DepositMyAssetProductName = "a", Note = "" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Create income missing main class fails model state cleanly.</summary>
    [Fact]
    public async Task CreateIncome_MissingMainClass_FailsModelStateCleanly()
    {
        var session = await LoginAsync();
        var payload = new { Id = 0, MainClass = (string?)null, SubClass = "LaborIncome", Content = "x", Amount = "1", DepositMyAssetProductName = "a", Note = "" };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateIncome", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
    }

    // -- CreateIncome: success path — verifies the Asset-balance side effect too -----

    /// <summary>Create income valid payload deposits into asset balance.</summary>
    [Fact]
    public async Task CreateIncome_ValidPayload_DepositsIntoAssetBalance()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        using (var scope = factory.Services.CreateScope())
            SeedAssetIfMissing(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), asset);
        decimal before = GetAssetBalance(asset);

        var payload = new
        {
            Id = 0, MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Bonus",
            Amount = "300", DepositMyAssetProductName = asset, Note = "",
            Created = "2026-01-15 10:30:00"
        };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateIncome", payload);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
        Assert.Equal(before + 300, GetAssetBalance(asset));
    }

    // -- UpdateIncome / DeleteIncome: success path ------------------------------------

    /// <summary>Update income existing record returns success result.</summary>
    [Fact]
    public async Task UpdateIncome_ExistingRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedIncome();

        using var request = session.BuildJsonPostRequest("/AccountBook/UpdateIncome", new
        {
            Id = id, MainClass = "RegularIncome", SubClass = "BusinessIncome", Content = "Updated",
            Amount = "1200", DepositMyAssetProductName = AssetProductName, Note = "updated",
            Created = "2026-01-15 10:30:00"
        });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var income = db.Incomes.Single(i => i.Id == id);
        Assert.Equal("BusinessIncome", income.SubClass);
        Assert.Equal("updated", income.Note);
    }

    /// <summary>Delete income existing record removes it and refunds asset balance.</summary>
    [Fact]
    public async Task DeleteIncome_ExistingRecord_RemovesItAndRefundsAssetBalance()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        long id = SeedIncome(amount: 500, productName: asset);
        decimal balanceAfterDeposit = GetAssetBalance(asset);

        using var request = session.BuildJsonPostRequest("/AccountBook/DeleteIncome", new { Id = id });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Incomes.Any(i => i.Id == id));
        Assert.Equal(balanceAfterDeposit - 500, GetAssetBalance(asset));
    }

    // -- IsIncomeExists -------------------------------------------------------------

    /// <summary>Is income exists seeded record returns success result.</summary>
    [Fact]
    public async Task IsIncomeExists_SeededRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedIncome();

        using var request = session.BuildJsonPostRequest($"/AccountBook/IsIncomeExists?id={id}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
    }

    /// <summary>Is income exists unknown id returns failure result.</summary>
    [Fact]
    public async Task IsIncomeExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/IsIncomeExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
    }

    /// <summary>Get income amount label unknown product returns base label.</summary>
    [Fact]
    public async Task GetIncomeAmountLabel_UnknownProduct_ReturnsBaseLabel()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/GetIncomeAmountLabel?productName=no-such-asset");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
    }

    // -- CreateExpenditure: role gating + ModelState validation --------------------

    /// <summary>Create expenditure missing payment method fails model state cleanly.</summary>
    [Fact]
    public async Task CreateExpenditure_MissingPaymentMethod_FailsModelStateCleanly()
    {
        var session = await LoginAsync();
        var payload = new { Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "x", Amount = "1", PaymentMethod = (string?)null, MyDepositAsset = "a", Note = "" };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateExpenditure", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
    }

    // -- CreateExpenditure: success path — verifies the Asset-balance side effect ----

    /// <summary>Create expenditure valid payload withdraws from asset balance.</summary>
    [Fact]
    public async Task CreateExpenditure_ValidPayload_WithdrawsFromAssetBalance()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        using (var scope = factory.Services.CreateScope())
            SeedAssetIfMissing(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), asset);
        decimal before = GetAssetBalance(asset);

        var payload = new
        {
            Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Dinner",
            Amount = "40", PaymentMethod = asset, MyDepositAsset = "N/A", Note = "",
            Created = "2026-01-15 10:30:00"
        };
        using var request = session.BuildJsonPostRequest("/AccountBook/CreateExpenditure", payload);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
        Assert.Equal(before - 40, GetAssetBalance(asset));
    }

    // -- UpdateExpenditure / DeleteExpenditure: success path --------------------------

    /// <summary>Update expenditure existing record returns success result.</summary>
    [Fact]
    public async Task UpdateExpenditure_ExistingRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedExpenditure();

        using var request = session.BuildJsonPostRequest("/AccountBook/UpdateExpenditure", new
        {
            Id = id, MainClass = "ConsumerSpending", SubClass = "TransportationCost", Content = "Updated",
            Amount = "60", PaymentMethod = AssetProductName, MyDepositAsset = "N/A", Note = "updated",
            Created = "2026-01-15 10:30:00"
        });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expenditure = db.Expenditures.Single(e => e.Id == id);
        Assert.Equal("TransportationCost", expenditure.SubClass);
        Assert.Equal("updated", expenditure.Note);
    }

    /// <summary>Delete expenditure existing record removes it and refunds asset balance.</summary>
    [Fact]
    public async Task DeleteExpenditure_ExistingRecord_RemovesItAndRefundsAssetBalance()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        long id = SeedExpenditure(amount: 75, productName: asset);
        decimal balanceAfterWithdrawal = GetAssetBalance(asset);

        using var request = session.BuildJsonPostRequest("/AccountBook/DeleteExpenditure", new { Id = id });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Expenditures.Any(e => e.Id == id));
        Assert.Equal(balanceAfterWithdrawal + 75, GetAssetBalance(asset));
    }

    // -- Amount decimal places (regression: balance drift) -----------------------------

    /// <summary>
    /// Regression test: amounts used to be stored as numeric(18,2), so a 0.005 expenditure was saved
    /// as 0.01 while the balance 99.995 was saved as 100.00 — deleting the expenditure then refunded
    /// 0.01 and the balance grew to 100.01, a little more on every create/delete. Four decimals are now
    /// stored exactly, so the balance returns to exactly where it started.
    /// </summary>
    [Fact]
    public async Task CreateThenDeleteExpenditure_OfAFractionalAmount_RestoresTheBalanceExactly()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedAssetIfMissing(db, asset);
            db.Assets.Single(a => a.AccountEmail == Email && a.ProductName == asset).Amount = 100.00m;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var create = session.BuildJsonPostRequest("/AccountBook/CreateExpenditure", new
        {
            Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Gum",
            Amount = "0.005", PaymentMethod = asset, MyDepositAsset = "N/A", Note = "", Created = "2026-01-15 10:30:00"
        }))
            Assert.Contains("\"result\":true", await (await _client.SendAsync(create, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        long id;
        using (var scope = factory.Services.CreateScope())
        {
            var stored = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Expenditures.Single(e => e.AccountEmail == Email && e.PaymentMethod == asset);
            Assert.Equal(0.005m, stored.Amount);
            id = stored.Id;
        }
        Assert.Equal(99.995m, GetAssetBalance(asset));

        using (var delete = session.BuildJsonPostRequest("/AccountBook/DeleteExpenditure", new { Id = id }))
            Assert.Contains("\"result\":true", await (await _client.SendAsync(delete, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        Assert.Equal(100.00m, GetAssetBalance(asset));
    }

    /// <summary>A fifth decimal place is refused with its own message (never rounded), and the balance is untouched.</summary>
    [Fact]
    public async Task CreateExpenditure_WithFiveDecimalPlaces_IsRejected_AndLeavesTheBalanceAlone()
    {
        string asset = UniqueAssetName();
        var session = await LoginAsync();
        using (var scope = factory.Services.CreateScope())
            SeedAssetIfMissing(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), asset);
        decimal before = GetAssetBalance(asset);

        using var request = session.BuildJsonPostRequest("/AccountBook/CreateExpenditure", new
        {
            Id = 0, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = "Gum",
            Amount = "0.00005", PaymentMethod = asset, MyDepositAsset = "N/A", Note = "", Created = "2026-01-15 10:30:00"
        });
        string json = await (await _client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
        Assert.Contains("Amount can have up to 4 decimal places.", json, StringComparison.Ordinal);
        Assert.Equal(before, GetAssetBalance(asset));
    }

    // -- IsExpenditureExists ----------------------------------------------------------

    /// <summary>Is expenditure exists seeded record returns success result.</summary>
    [Fact]
    public async Task IsExpenditureExists_SeededRecord_ReturnsSuccessResult()
    {
        var session = await LoginAsync();
        long id = SeedExpenditure();

        using var request = session.BuildJsonPostRequest($"/AccountBook/IsExpenditureExists?id={id}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
    }

    /// <summary>Is expenditure exists unknown id returns failure result.</summary>
    [Fact]
    public async Task IsExpenditureExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/IsExpenditureExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
    }

    /// <summary>Get expenditure amount label unknown product returns base label.</summary>
    [Fact]
    public async Task GetExpenditureAmountLabel_UnknownProduct_ReturnsBaseLabel()
    {
        var session = await LoginAsync();
        using var request = session.BuildJsonPostRequest("/AccountBook/GetExpenditureAmountLabel?productName=no-such-asset");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json, StringComparison.Ordinal);
    }

    // -- SuggestExpenditureClass -------------------------------------------------------

    /// <summary>Asks <paramref name="path"/> for a suggestion for <paramref name="content"/>; returns the JSON reply.</summary>
    private async Task<string> SuggestAsync(AuthenticatedSession session, string path, string content)
    {
        using var request = session.BuildJsonPostRequest($"{path}?content={Uri.EscapeDataString(content)}");
        using HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The suggestion is the signed-in user's own model, trained by the worker's training on their expenditures in the real database:
    /// the first question asks for the model, and then new content about the subway is answered with transport. A dedicated account
    /// keeps the other tests' records out of the model.
    /// </summary>
    [Fact]
    public async Task SuggestExpenditureClass_NewContent_IsAnsweredFromTheUsersOwnRecords()
    {
        const string email = "accountbook-suggest-expenditure@test.com";
        var session = await AuthenticatedSessionHelper.LoginAsync(factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Assets.Any(a => a.AccountEmail == email))
                db.Assets.Add(new Asset { ProductName = "wallet", AccountEmail = email, Item = "CashAsset", Amount = 0, MonetaryUnit = "KRW", Deleted = false, Created = DateTime.UtcNow, Updated = DateTime.UtcNow });
            db.Expenditures.RemoveRange(db.Expenditures.Where(e => e.AccountEmail == email));
            foreach ((string content, string subClass) in new[] { ("지하철 교통카드", "TransportationCost"), ("지하철 정기권", "TransportationCost"), ("스타벅스 강남", "MealOrEatOutExpenses"), ("스타벅스 역삼", "MealOrEatOutExpenses") })
            {
                db.Expenditures.Add(new Expenditure
                {
                    AccountEmail = email, MainClass = "ConsumerSpending", SubClass = subClass, Content = content, Amount = 1000,
                    PaymentMethod = "wallet", Created = DateTime.UtcNow, Updated = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // The first question finds no model and asks the worker for one (here trained at once); the next one is answered by it.
        string first = await SuggestAsync(session, "/AccountBook/SuggestExpenditureClass", "지하철 2호선");
        string second = await SuggestAsync(session, "/AccountBook/SuggestExpenditureClass", "지하철 2호선");

        Assert.Equal("""{"result":true,"suggestion":null}""", first);
        Assert.Equal("""{"result":true,"suggestion":{"mainClass":"ConsumerSpending","subClass":"TransportationCost"}}""", second);
    }

    /// <summary>Content that matches none of the user's rows succeeds with no suggestion (null), so the form shows nothing.</summary>
    [Fact]
    public async Task SuggestExpenditureClass_NothingMatches_ReturnsNoSuggestion()
    {
        var session = await LoginAsync();

        using var request = session.BuildJsonPostRequest("/AccountBook/SuggestExpenditureClass?content=%E2%80%A6%21");
        string json = await (await _client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal("""{"result":true,"suggestion":null}""", json);
    }

    /// <summary>
    /// The create form carries the hidden suggestion hint the page script fills (text + apply button), labeled through the
    /// page's resx: without it the script would have nowhere to show a suggestion.
    /// </summary>
    [Fact]
    public async Task ExpenditurePage_RendersTheHiddenClassSuggestionHint_InKorean()
    {
        var session = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/AccountBook/Expenditure");
        request.Headers.Add("Cookie", session.CookieHeader);
        request.Headers.AcceptLanguage.ParseAdd("ko-KR");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var page = await new AngleSharp.Html.Parser.HtmlParser().ParseDocumentAsync(html, TestContext.Current.CancellationToken);

        var hint = page.GetElementById("createExpenditureClassSuggestion");
        Assert.NotNull(hint);
        Assert.Contains("display:none", hint.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("추천 분류:", hint.TextContent, StringComparison.Ordinal);
        Assert.NotNull(hint.QuerySelector("#createExpenditureClassSuggestionText"));
        Assert.Equal("적용", hint.QuerySelector("#btnApplyExpenditureClassSuggestion")?.TextContent.Trim());
        Assert.NotNull(page.GetElementById("formCreateExpenditure")?.QuerySelector("#createExpenditureClassSuggestion"));
    }

    /// <summary>
    /// The expenditure grid marks, in Korean, the one amount far above the account's usual meals and no other row. A dedicated
    /// account keeps the other tests' meal records out of its "usual".
    /// </summary>
    [Fact]
    public async Task ExpenditureGrid_MarksTheUnusuallyLargeAmount()
    {
        const string email = "accountbook-outlier-user@test.com";
        var session = await AuthenticatedSessionHelper.LoginAsync(factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);
        long flagged;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Assets.Any(a => a.AccountEmail == email && a.ProductName == "wallet"))
            {
                db.Assets.Add(new Asset
                {
                    ProductName = "wallet", AccountEmail = email, Item = "CashAsset", Amount = 1000, MonetaryUnit = "KRW",
                    Deleted = false, Created = DateTime.UtcNow, Updated = DateTime.UtcNow
                });
            }
            db.Expenditures.RemoveRange(db.Expenditures.Where(e => e.AccountEmail == email));
            List<Expenditure> rows =
            [
                .. new decimal[] { 10, 10, 11, 12, 12, 13, 14, 100 }.Select(amount => new Expenditure
                {
                    AccountEmail = email, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses", Content = $"meal {amount}",
                    Amount = amount, PaymentMethod = "wallet", Created = DateTime.UtcNow, Updated = DateTime.UtcNow
                })
            ];
            db.Expenditures.AddRange(rows);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            flagged = rows[^1].Id;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/AccountBook/Expenditure");
        request.Headers.Add("Cookie", session.CookieHeader);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.AcceptLanguage.ParseAdd("ko-KR");
        string html = await (await _client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var page = await new AngleSharp.Html.Parser.HtmlParser().ParseDocumentAsync(html, TestContext.Current.CancellationToken);

        var rowsWithMark = page.QuerySelectorAll("tr.clsGridRow").Where(r => r.TextContent.Contains("평소보다 큼", StringComparison.Ordinal)).ToList();
        Assert.Equal(flagged.ToString(System.Globalization.CultureInfo.InvariantCulture), Assert.Single(rowsWithMark).GetAttribute("data-id"));
        Assert.Contains(page.QuerySelectorAll("th"), th => th.TextContent.Contains("금액 확인", StringComparison.Ordinal));
    }

    /// <summary>
    /// The income suggestion is the signed-in user's own income model, trained by the worker's training on their incomes in the real
    /// database: the first question asks for the model, and then new content about a salary is answered with labor income. A dedicated
    /// account keeps the other tests' records out of the model.
    /// </summary>
    [Fact]
    public async Task SuggestIncomeClass_NewContent_IsAnsweredFromTheUsersOwnRecords()
    {
        const string email = "accountbook-suggest-income@test.com";
        var session = await AuthenticatedSessionHelper.LoginAsync(factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Assets.Any(a => a.AccountEmail == email))
                db.Assets.Add(new Asset { ProductName = "wallet", AccountEmail = email, Item = "CashAsset", Amount = 0, MonetaryUnit = "KRW", Deleted = false, Created = DateTime.UtcNow, Updated = DateTime.UtcNow });
            db.Incomes.RemoveRange(db.Incomes.Where(i => i.AccountEmail == email));
            foreach ((string content, string subClass) in new[] { ("11월 급여", "LaborIncome"), ("10월 급여", "LaborIncome"), ("예금 이자", "FinancialIncome"), ("적금 이자", "FinancialIncome") })
            {
                db.Incomes.Add(new Income
                {
                    AccountEmail = email, MainClass = "RegularIncome", SubClass = subClass, Content = content, Amount = 1000,
                    DepositMyAssetProductName = "wallet", Created = DateTime.UtcNow, Updated = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // The first question finds no model and asks the worker for one (here trained at once); the next one is answered by it.
        string first = await SuggestAsync(session, "/AccountBook/SuggestIncomeClass", "12월 급여");
        string second = await SuggestAsync(session, "/AccountBook/SuggestIncomeClass", "12월 급여");

        Assert.Equal("""{"result":true,"suggestion":null}""", first);
        Assert.Equal("""{"result":true,"suggestion":{"mainClass":"RegularIncome","subClass":"LaborIncome"}}""", second);
    }

    /// <summary>Income content without a letter or a digit succeeds with no suggestion (null), so the form shows nothing.</summary>
    [Fact]
    public async Task SuggestIncomeClass_ContentWithoutEvidence_ReturnsNoSuggestion()
    {
        var session = await LoginAsync();

        using var request = session.BuildJsonPostRequest("/AccountBook/SuggestIncomeClass?content=%E2%80%A6%21");
        string json = await (await _client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal("""{"result":true,"suggestion":null}""", json);
    }

    /// <summary>The income create form carries the hidden suggestion hint the page script fills, labeled through the page's resx.</summary>
    [Fact]
    public async Task IncomePage_RendersTheHiddenClassSuggestionHint_InKorean()
    {
        var session = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/AccountBook/Income");
        request.Headers.Add("Cookie", session.CookieHeader);
        request.Headers.AcceptLanguage.ParseAdd("ko-KR");

        string html = await (await _client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var page = await new AngleSharp.Html.Parser.HtmlParser().ParseDocumentAsync(html, TestContext.Current.CancellationToken);

        var hint = page.GetElementById("formCreateIncome")?.QuerySelector("#createIncomeClassSuggestion");
        Assert.NotNull(hint);
        Assert.Contains("display:none", hint.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("추천 분류:", hint.TextContent, StringComparison.Ordinal);
        Assert.NotNull(hint.QuerySelector("#createIncomeClassSuggestionText"));
        Assert.Equal("적용", hint.QuerySelector("#btnApplyIncomeClassSuggestion")?.TextContent.Trim());
    }
}
