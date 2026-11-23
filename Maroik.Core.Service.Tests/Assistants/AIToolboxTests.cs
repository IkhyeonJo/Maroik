using System.Text.Json;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Assistants;
using Maroik.Core.Service.Assistants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Maroik.Core.Service.Tests.Assistants;

/// <summary>
/// The read-only tools of the AI chat (<see cref="AIToolbox"/>). The finance services are mocked; every tool reads only the signed-in
/// account (the model never names one), filters by the days the server resolved in the account's time zone, and answers with totals the
/// server added up. A refused argument goes back to the model as an <c>error</c> so it can try again.
/// </summary>
public class AIToolboxTests
{
    /// <summary>The signed-in account.</summary>
    private const string Email = "user@example.com";

    /// <summary>The account's time zone: Seoul, nine hours ahead of UTC.</summary>
    private const string Seoul = "Asia/Seoul";

    /// <summary>Today in Seoul: Wednesday 2026-11-25.</summary>
    private static readonly DateOnly _today = new(2026, 11, 25);

    /// <summary>The asset service.</summary>
    private readonly Mock<IAssetService> _assets = new(MockBehavior.Strict);

    /// <summary>The income service.</summary>
    private readonly Mock<IIncomeService> _incomes = new(MockBehavior.Strict);

    /// <summary>The expenditure service.</summary>
    private readonly Mock<IExpenditureService> _expenditures = new(MockBehavior.Strict);

    /// <summary>The fixed income service.</summary>
    private readonly Mock<IFixedIncomeService> _fixedIncomes = new(MockBehavior.Strict);

    /// <summary>The fixed expenditure service.</summary>
    private readonly Mock<IFixedExpenditureService> _fixedExpenditures = new(MockBehavior.Strict);

    /// <summary>Captures the log entries.</summary>
    private readonly FakeLogger<AIToolbox> _logger = new();

    /// <summary>The toolbox under test.</summary>
    private AIToolbox CreateSut() => new(_assets.Object, _incomes.Object, _expenditures.Object, _fixedIncomes.Object, _fixedExpenditures.Object, _logger);

    /// <summary>Runs <paramref name="tool"/> with <paramref name="arguments"/> and parses its answer.</summary>
    private async Task<JsonElement> RunAsync(string tool, string arguments = "{}")
    {
        string json = await CreateSut().RunAsync(new LlmToolCall(tool, arguments), Email, Seoul, _today, TestContext.Current.CancellationToken);
        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>An expenditure stored at <paramref name="createdUtc"/>.</summary>
    private static ExpenditureResponse Expenditure(DateTime createdUtc, decimal amount, string currency = "KRW", string mainClass = "ConsumerSpending",
        string subClass = "MealOrEatOutExpenses", string content = "Lunch", string? note = null) => new()
    {
        Created = createdUtc, Amount = amount, MonetaryUnit = currency, MainClass = mainClass, SubClass = subClass, Content = content,
        PaymentMethod = "Card", Note = note, AccountEmail = Email,
    };

    /// <summary>Makes the expenditure service answer <paramref name="rows"/> for the signed-in account only.</summary>
    private void Expenditures(params ExpenditureResponse[] rows) =>
        _expenditures.Setup(s => s.GetExpendituresAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync([.. rows]);

    /// <summary>The six tools are offered, each with a JSON Schema object for its arguments and a description.</summary>
    [Fact]
    public void Definitions_OfferTheSixReadOnlyTools()
    {
        Assert.Equal(
            ["get_assets", "get_incomes", "get_expenditures", "get_expenditure_totals_by_class", "get_fixed_incomes", "get_fixed_expenditures"],
            AIToolbox.Definitions.Select(d => d.Name));
        Assert.All(AIToolbox.Definitions, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Description));
            using JsonDocument schema = JsonDocument.Parse(d.ParametersJsonSchema);
            Assert.Equal("object", schema.RootElement.GetProperty("type").GetString());
        });
    }

    /// <summary>The period tools offer exactly the period names the domain resolves.</summary>
    [Fact]
    public void Definitions_OfferThePeriodNamesOfTheDomain()
    {
        using JsonDocument schema = JsonDocument.Parse(AIToolbox.Definitions.Single(d => d.Name == "get_expenditures").ParametersJsonSchema);

        Assert.Equal(AssistantPeriod.Names, schema.RootElement.GetProperty("properties").GetProperty("period").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
    }

    /// <summary>Whether a tool name is one of the tools.</summary>
    [Fact]
    public void IsKnown_TellsTheToolsApart()
    {
        Assert.True(AIToolbox.IsKnown("get_assets"));
        Assert.False(AIToolbox.IsKnown("delete_everything"));
    }

    /// <summary>
    /// The expenditures of the period are those whose time falls on its days in the account's time zone (a record stored on the evening
    /// of the last day before in UTC, already the first day in Seoul, counts); newest first, dated in local days, with the totals per currency.
    /// Every amount is written out with its currency ("9,000.5 KRW"), so the model copies it instead of formatting a number.
    /// </summary>
    [Fact]
    public async Task GetExpenditures_ReadsThePeriodInTheAccountsTimeZone_WithTotalsPerCurrency()
    {
        Expenditures(
            Expenditure(new DateTime(2026, 10, 31, 14, 59, 0), 1m, content: "Still October in Seoul"),
            Expenditure(new DateTime(2026, 10, 31, 15, 0, 0), 9000.50m, content: "First of November in Seoul"),
            Expenditure(new DateTime(2026, 11, 20, 3, 0, 0), 1000.25m),
            Expenditure(new DateTime(2026, 11, 21, 3, 0, 0), 12m, currency: "USD"),
            Expenditure(new DateTime(2026, 11, 30, 15, 0, 0), 5m, content: "Already December in Seoul"));

        JsonElement answer = await RunAsync("get_expenditures", """{"period":"this_month"}""");

        Assert.Equal("2026-11-01", answer.GetProperty("start_date").GetString());
        Assert.Equal("2026-11-30", answer.GetProperty("end_date").GetString());
        Assert.Equal(3, answer.GetProperty("count").GetInt32());
        JsonElement[] records = [.. answer.GetProperty("records").EnumerateArray()];
        Assert.Equal(["2026-11-21", "2026-11-20", "2026-11-01"], records.Select(r => r.GetProperty("date").GetString()));
        Assert.Equal("First of November in Seoul", records[2].GetProperty("content").GetString());
        Assert.Equal("9,000.5 KRW", records[2].GetProperty("amount").GetString());
        Assert.Equal("Card", records[2].GetProperty("payment_method").GetString());

        JsonElement[] totals = [.. answer.GetProperty("totals").EnumerateArray()];
        Assert.Equal(["10,000.75 KRW", "12 USD"], totals.Select(t => t.GetProperty("total").GetString()));
        Assert.Equal(2, totals[0].GetProperty("count").GetInt32());
    }

    /// <summary>A main class and a keyword narrow the records; the keyword matches the content, the sub-class or the note, ignoring case.</summary>
    [Fact]
    public async Task GetExpenditures_FiltersByMainClassAndKeyword()
    {
        DateTime day = new(2026, 11, 20, 3, 0, 0);
        Expenditures(
            Expenditure(day, 1m, content: "Coffee"),
            Expenditure(day, 2m, content: "Rent", mainClass: "NonConsumerSpending", subClass: "Housing"),
            Expenditure(day, 4m, content: "Snack", note: "coffee beans"),
            Expenditure(day, 8m, content: "Bus", subClass: "Transportation"));

        JsonElement byClass = await RunAsync("get_expenditures", """{"main_class":"NonConsumerSpending"}""");
        JsonElement byKeyword = await RunAsync("get_expenditures", """{"keyword":"COFFEE"}""");
        JsonElement bySubClass = await RunAsync("get_expenditures", """{"keyword":"transport"}""");

        Assert.Equal(["Rent"], byClass.GetProperty("records").EnumerateArray().Select(r => r.GetProperty("content").GetString()));
        Assert.Equal("5 KRW", byKeyword.GetProperty("totals")[0].GetProperty("total").GetString());
        Assert.Equal(["Bus"], bySubClass.GetProperty("records").EnumerateArray().Select(r => r.GetProperty("content").GetString()));
    }

    /// <summary>A sub-class narrows the records to it ("food" is MealOrEatOutExpenses, not all consumer spending).</summary>
    [Fact]
    public async Task GetExpenditures_FiltersBySubClass()
    {
        DateTime day = new(2026, 11, 20, 3, 0, 0);
        Expenditures(Expenditure(day, 38000m, content: "Dinner"), Expenditure(day, 120000m, content: "Dentist", subClass: "MedicalExpenses"));

        JsonElement answer = await RunAsync("get_expenditures", """{"sub_class":"MealOrEatOutExpenses"}""");

        Assert.Equal(["Dinner"], answer.GetProperty("records").EnumerateArray().Select(r => r.GetProperty("content").GetString()));
        Assert.Equal("38,000 KRW", answer.GetProperty("totals")[0].GetProperty("total").GetString());
    }

    /// <summary>The sub-classes offered are the domain's taxonomy of each book.</summary>
    [Fact]
    public void Definitions_OfferTheSubClassesOfEachBook()
    {
        string[] SubClasses(string tool)
        {
            using JsonDocument schema = JsonDocument.Parse(AIToolbox.Definitions.Single(d => d.Name == tool).ParametersJsonSchema);
            return [.. schema.RootElement.GetProperty("properties").GetProperty("sub_class").GetProperty("enum").EnumerateArray().Select(e => e.GetString()!)];
        }

        Assert.Equal(Enum.GetNames<Domain.Finances.ExpenditureSubClass>(), SubClasses("get_expenditures"));
        Assert.Equal(Enum.GetNames<Domain.Finances.IncomeSubClass>(), SubClasses("get_incomes"));
    }

    /// <summary>
    /// Only <see cref="AssistantPolicy.MaxToolRows"/> records are listed (the newest), but the count and the totals cover every record of
    /// the period.
    /// </summary>
    [Fact]
    public async Task GetExpenditures_ListsAtMostTheRowLimit_ButTotalsEveryRecord()
    {
        Expenditures([.. Enumerable.Range(1, AssistantPolicy.MaxToolRows + 5).Select(i => Expenditure(new DateTime(2026, 11, 1, 0, i, 0), 1m))]);

        JsonElement answer = await RunAsync("get_expenditures", """{"period":"this_month"}""");

        Assert.Equal(AssistantPolicy.MaxToolRows + 5, answer.GetProperty("count").GetInt32());
        Assert.Equal(AssistantPolicy.MaxToolRows, answer.GetProperty("records").GetArrayLength());
        Assert.Equal($"{AssistantPolicy.MaxToolRows + 5} KRW", answer.GetProperty("totals")[0].GetProperty("total").GetString());
    }

    /// <summary>The expenditure totals are added up per main class and sub-class, per currency, the largest first.</summary>
    [Fact]
    public async Task GetExpenditureTotalsByClass_AddsUpPerClassAndCurrency()
    {
        DateTime day = new(2026, 11, 20, 3, 0, 0);
        Expenditures(
            Expenditure(day, 100m),
            Expenditure(day, 50m),
            Expenditure(day, 700m, mainClass: "NonConsumerSpending", subClass: "Housing"),
            Expenditure(new DateTime(2025, 1, 1), 999m));

        JsonElement answer = await RunAsync("get_expenditure_totals_by_class", """{"period":"this_year"}""");

        JsonElement[] totals = [.. answer.GetProperty("totals").EnumerateArray()];
        Assert.Equal(["NonConsumerSpending / Housing", "ConsumerSpending / MealOrEatOutExpenses"], totals.Select(t => t.GetProperty("class").GetString()));
        Assert.Equal("150 KRW", totals[1].GetProperty("total").GetString());
        Assert.Equal(2, totals[1].GetProperty("count").GetInt32());
        Assert.Equal("2026-01-01", answer.GetProperty("start_date").GetString());
    }

    /// <summary>The incomes of a period of two explicit dates, with the asset each was deposited to.</summary>
    [Fact]
    public async Task GetIncomes_ReadsAnExplicitPeriod()
    {
        _incomes.Setup(s => s.GetIncomesAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new IncomeResponse { Created = new DateTime(2026, 3, 1, 15, 0, 0), Amount = 3000000m, MonetaryUnit = "KRW", MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Salary", DepositMyAssetProductName = "Bank" },
            new IncomeResponse { Created = new DateTime(2026, 4, 1, 15, 0, 0), Amount = 1m, MonetaryUnit = "KRW", MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "Later" },
        ]);

        JsonElement answer = await RunAsync("get_incomes", """{"start_date":"2026-03-01","end_date":"2026-03-31"}""");

        JsonElement record = Assert.Single(answer.GetProperty("records").EnumerateArray());
        Assert.Equal("2026-03-02", record.GetProperty("date").GetString());
        Assert.Equal("Bank", record.GetProperty("deposit_asset").GetString());
        Assert.Equal("3,000,000 KRW", answer.GetProperty("totals")[0].GetProperty("total").GetString());
    }

    /// <summary>The assets that are not deleted, with their totals per currency.</summary>
    [Fact]
    public async Task GetAssets_ListsTheLiveAssets_WithTotalsPerCurrency()
    {
        _assets.Setup(s => s.GetAssetsAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new AssetResponse { ProductName = "Bank", Item = "Account", Amount = 1000m, MonetaryUnit = "KRW" },
            new AssetResponse { ProductName = "Wallet", Item = "Cash", Amount = 500.5m, MonetaryUnit = "KRW" },
            new AssetResponse { ProductName = "Closed", Item = "Account", Amount = 7m, MonetaryUnit = "KRW", Deleted = true },
        ]);

        JsonElement answer = await RunAsync("get_assets");

        Assert.Equal(["Bank", "Wallet"], answer.GetProperty("assets").EnumerateArray().Select(a => a.GetProperty("name").GetString()));
        Assert.Equal("1,500.5 KRW", answer.GetProperty("totals")[0].GetProperty("total").GetString());
        Assert.Equal("1,000 KRW", answer.GetProperty("assets")[0].GetProperty("balance").GetString());
    }

    /// <summary>The fixed incomes and expenditures still running (not past their maturity in the account's today), with the scheduled date.</summary>
    [Fact]
    public async Task GetFixedSchedules_ListOnlyTheRunningOnes()
    {
        _fixedIncomes.Setup(s => s.GetFixedIncomesAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new FixedIncomeResponse { Content = "Salary", Amount = 3000000m, MonetaryUnit = "KRW", MainClass = "RegularIncome", SubClass = "LaborIncome", DepositMonth = 12, DepositDay = 25, MaturityDate = new DateTime(9999, 12, 31) },
            new FixedIncomeResponse { Content = "Ended", Amount = 1m, MonetaryUnit = "KRW", MaturityDate = new DateTime(2026, 11, 24) },
        ]);
        _fixedExpenditures.Setup(s => s.GetFixedExpendituresAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new FixedExpenditureResponse { Content = "Rent", Amount = 500000m, MonetaryUnit = "KRW", MainClass = "NonConsumerSpending", SubClass = "Housing", PaymentMethod = "Bank", DepositMonth = 12, DepositDay = 1, MaturityDate = new DateTime(2026, 11, 25) },
        ]);

        JsonElement incomes = await RunAsync("get_fixed_incomes");
        JsonElement expenditures = await RunAsync("get_fixed_expenditures");

        JsonElement salary = Assert.Single(incomes.GetProperty("schedules").EnumerateArray());
        Assert.Equal("Salary", salary.GetProperty("content").GetString());
        Assert.Equal(12, salary.GetProperty("deposit_month").GetInt32());
        Assert.Equal(25, salary.GetProperty("deposit_day").GetInt32());
        Assert.Equal(JsonValueKind.Null, salary.GetProperty("maturity_date").ValueKind);
        JsonElement rent = Assert.Single(expenditures.GetProperty("schedules").EnumerateArray());
        Assert.Equal("2026-11-25", rent.GetProperty("maturity_date").GetString());
        Assert.Equal("Bank", rent.GetProperty("payment_method").GetString());
        Assert.Equal("500,000 KRW", expenditures.GetProperty("totals")[0].GetProperty("total").GetString());
        Assert.Equal("3,000,000 KRW", salary.GetProperty("amount").GetString());
    }

    /// <summary>
    /// A refused argument is answered with the reason, for the model to try again, and logged at Information with the tool and the code
    /// (not the arguments, which may hold the user's words).
    /// </summary>
    [Theory]
    [InlineData("get_expenditures", """{"period":"next_decade"}""", "Unknown period")]
    [InlineData("get_expenditures", """{"main_class":"Groceries","keyword":"PRIVATE"}""", "main_class must be one of")]
    [InlineData("get_incomes", """{"main_class":"ConsumerSpending"}""", "main_class must be one of")]
    [InlineData("get_expenditures", """{"sub_class":"LaborIncome"}""", "sub_class must be one of")]
    [InlineData("get_incomes", """{"sub_class":"MealOrEatOutExpenses"}""", "sub_class must be one of")]
    [InlineData("get_expenditures", "not json PRIVATE", "Arguments must be a JSON object")]
    [InlineData("get_expenditures", """["PRIVATE"]""", "Arguments must be a JSON object")]
    [InlineData("delete_everything", "{}", "Unknown tool")]
    public async Task ARefusedCall_IsAnsweredWithItsReason(string tool, string arguments, string reason)
    {
        JsonElement answer = await RunAsync(tool, arguments);

        Assert.Contains(reason, answer.GetProperty("error").GetString(), StringComparison.Ordinal);
        FakeLogRecord log = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Contains(tool, log.Message, StringComparison.Ordinal);
        Assert.Contains(Email, log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE", log.Message, StringComparison.Ordinal);
    }

    /// <summary>A blank or JSON-null optional argument counts as not given.</summary>
    [Fact]
    public async Task BlankArguments_CountAsNotGiven()
    {
        Expenditures(Expenditure(new DateTime(2026, 11, 20, 3, 0, 0), 1m));

        JsonElement answer = await RunAsync("get_expenditures", """{"period":"","main_class":null,"keyword":" ","start_date":null}""");

        Assert.Equal(1, answer.GetProperty("count").GetInt32());
        Assert.Equal("2026-11-01", answer.GetProperty("start_date").GetString());
    }
}
