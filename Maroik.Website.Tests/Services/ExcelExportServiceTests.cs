using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Website.Services;

namespace Maroik.Website.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ExcelExportService"/>.
/// Each produced workbook is re-opened with the same DocumentFormat.OpenXml library it was
/// written with, and assertions are made against the actual header/row cell content, ordering,
/// and number/date formatting rather than merely "no exception was thrown".
/// </summary>
public class ExcelExportServiceTests
{
    private readonly ExcelExportService _sut = new();

    private static readonly Func<string, string> Localize = key => $"L:{key}";
    private static readonly Func<string, string> Identity = key => key;

    /// <summary>Reads every row (header + data) of the first worksheet as raw cell text.</summary>
    private static List<List<string>> ReadRows(MemoryStream stream)
    {
        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, false);
        var worksheetPart = document.WorkbookPart!.WorksheetParts.First();
        var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().First() ?? [];
        return
        [
            .. sheetData.Elements<Row>()
                .Select(row => row.Elements<Cell>().Select(c => c.CellValue?.Text ?? "").ToList())
        ];
    }

    // ── CreateAssetExcel ──────────────────────────────────────────────────────

    /// <summary>Create asset excel localizes headers.</summary>
    [Fact]
    public void CreateAssetExcel_LocalizesHeaders()
    {
        var stream = _sut.CreateAssetExcel([], Localize, "UTC");

        var rows = ReadRows(stream);

        Assert.Equal(
            ["L:ProductName", "L:Item", "L:Amount", "L:MonetaryUnit", "L:Created", "L:Updated", "L:Note", "L:Deleted"],
            rows[0]);
    }

    /// <summary>Create asset excel orders rows by product name ascending.</summary>
    [Fact]
    public void CreateAssetExcel_OrdersRowsByProductNameAscending()
    {
        AssetResponse[] items =
        [
            new() { ProductName = "Zeta", Item = "z", Amount = 1m, MonetaryUnit = "KRW", Note = "" },
            new() { ProductName = "Alpha", Item = "a", Amount = 2m, MonetaryUnit = "KRW", Note = "" }
        ];

        var rows = ReadRows(_sut.CreateAssetExcel(items, Identity, "UTC"));

        Assert.Equal("Alpha", rows[1][0]);
        Assert.Equal("Zeta", rows[2][0]);
    }

    /// <summary>Create asset excel localizes item column and trims amount trailing zeros.</summary>
    [Fact]
    public void CreateAssetExcel_LocalizesItemColumn_AndTrimsAmountTrailingZeros()
    {
        AssetResponse[] items = [new() { ProductName = "Cash", Item = "Deposit", Amount = 1000.00m, MonetaryUnit = "KRW", Note = "" }];

        var row = ReadRows(_sut.CreateAssetExcel(items, Localize, "UTC"))[1];

        Assert.Equal("Cash", row[0]);
        Assert.Equal("L:Deposit", row[1]);
        Assert.Equal("1000", row[2]);
        Assert.Equal("KRW", row[3]);
    }

    /// <summary>Create asset excel converts created and updated to the given time zone.</summary>
    [Fact]
    public void CreateAssetExcel_ConvertsCreatedAndUpdatedToTheGivenTimeZone()
    {
        var created = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2025, 6, 10, 15, 0, 0, DateTimeKind.Utc);
        AssetResponse[] items = [new() { ProductName = "Cash", Item = "Deposit", Amount = 1m, Note = "" , Created = created, Updated = updated}];

        var row = ReadRows(_sut.CreateAssetExcel(items, Identity, "Asia/Seoul"))[1];

        Assert.Equal(created.ConvertTimeByTimeZoneIanaId("Asia/Seoul").ToString(CultureInfo.InvariantCulture), row[4]);
        Assert.Equal(updated.ConvertTimeByTimeZoneIanaId("Asia/Seoul").ToString(CultureInfo.InvariantCulture), row[5]);
        // Asia/Seoul is UTC+9 with no daylight saving: 00:00 UTC -> 09:00, 15:00 UTC -> next-day 00:00.
        Assert.Contains("09:00:00", row[4]);
        Assert.Contains("00:00:00", row[5]);
    }

    /// <summary>Create asset excel renders note and deleted flag.</summary>
    [Fact]
    public void CreateAssetExcel_RendersNoteAndDeletedFlag()
    {
        AssetResponse[] items = [new() { ProductName = "Cash", Item = "x", Amount = 1m, Note = "closed account", Deleted = true }];

        var row = ReadRows(_sut.CreateAssetExcel(items, Identity, "UTC"))[1];

        Assert.Equal("closed account", row[6]);
        Assert.Equal("True", row[7]);
    }

    // ── CreateIncomeExcel ─────────────────────────────────────────────────────

    /// <summary>Create income excel localizes headers and class fields.</summary>
    [Fact]
    public void CreateIncomeExcel_LocalizesHeadersAndClassFields()
    {
        IncomeResponse[] items = [new() { MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "salary", Amount = 100m }];

        var rows = ReadRows(_sut.CreateIncomeExcel(items, [], Localize, "UTC"));

        Assert.Equal("L:MainClass", rows[0][0]);
        Assert.Equal("L:RegularIncome", rows[1][0]);
        Assert.Equal("L:LaborIncome", rows[1][1]);
        Assert.Equal("salary", rows[1][2]);
    }

    /// <summary>Create income excel populates monetary unit from matching deposit asset.</summary>
    [Fact]
    public void CreateIncomeExcel_PopulatesMonetaryUnitFromMatchingDepositAsset()
    {
        AssetResponse[] assets = [new() { ProductName = "Bank", MonetaryUnit = "KRW" }];
        IncomeResponse[] items = [new() { Content = "salary", Amount = 100m, DepositMyAssetProductName = "Bank" }];

        var row = ReadRows(_sut.CreateIncomeExcel(items, assets, Identity, "UTC"))[1];

        Assert.Equal("KRW", row[4]);
        Assert.Equal("Bank", row[5]);
    }

    /// <summary>Create income excel no matching asset monetary unit is empty.</summary>
    [Fact]
    public void CreateIncomeExcel_NoMatchingAsset_MonetaryUnitIsEmpty()
    {
        IncomeResponse[] items = [new() { Content = "salary", Amount = 100m, DepositMyAssetProductName = "Unknown" }];

        var row = ReadRows(_sut.CreateIncomeExcel(items, [], Identity, "UTC"))[1];

        Assert.Equal("", row[4]);
    }

    /// <summary>Create income excel orders by created then updated descending.</summary>
    [Fact]
    public void CreateIncomeExcel_OrdersByCreatedThenUpdatedDescending()
    {
        IncomeResponse[] items =
        [
            new() { Content = "older", Amount = 1m, Created = new DateTime(2025, 1, 1) },
            new() { Content = "newer", Amount = 2m, Created = new DateTime(2025, 6, 1) }
        ];

        var rows = ReadRows(_sut.CreateIncomeExcel(items, [], Identity, "UTC"));

        Assert.Equal("newer", rows[1][2]);
        Assert.Equal("older", rows[2][2]);
    }

    /// <summary>Create income excel trims amount trailing zeros.</summary>
    [Fact]
    public void CreateIncomeExcel_TrimsAmountTrailingZeros()
    {
        IncomeResponse[] items = [new() { Content = "bonus", Amount = 500.50m }];

        var row = ReadRows(_sut.CreateIncomeExcel(items, [], Identity, "UTC"))[1];

        Assert.Equal("500.5", row[3]);
    }

    // ── CreateExpenditureExcel ────────────────────────────────────────────────

    /// <summary>Create expenditure excel localizes headers and class fields.</summary>
    [Fact]
    public void CreateExpenditureExcel_LocalizesHeadersAndClassFields()
    {
        ExpenditureResponse[] items = [new() { MainClass = "ConsumerSpending", SubClass = "Tax", Content = "income tax", Amount = 50m }];

        var rows = ReadRows(_sut.CreateExpenditureExcel(items, [], Localize, "UTC"));

        Assert.Equal("L:MainClass", rows[0][0]);
        Assert.Equal("L:ConsumerSpending", rows[1][0]);
        Assert.Equal("L:Tax", rows[1][1]);
    }

    /// <summary>Create expenditure excel populates monetary unit from asset matching payment method.</summary>
    [Fact]
    public void CreateExpenditureExcel_PopulatesMonetaryUnitFromAssetMatchingPaymentMethod()
    {
        // NB: production code keys the currency lookup by PaymentMethod (asset product name), not MyDepositAsset.
        AssetResponse[] assets = [new() { ProductName = "CreditCard", MonetaryUnit = "USD" }];
        ExpenditureResponse[] items = [new() { Content = "lunch", Amount = 10m, PaymentMethod = "CreditCard", MyDepositAsset = "Bank" }];

        var row = ReadRows(_sut.CreateExpenditureExcel(items, assets, Identity, "UTC"))[1];

        Assert.Equal("USD", row[4]);
        Assert.Equal("CreditCard", row[5]);
        Assert.Equal("Bank", row[7]);
    }

    /// <summary>Create expenditure excel orders by created then updated descending.</summary>
    [Fact]
    public void CreateExpenditureExcel_OrdersByCreatedThenUpdatedDescending()
    {
        ExpenditureResponse[] items =
        [
            new() { Content = "older", Amount = 1m, Created = new DateTime(2025, 1, 1) },
            new() { Content = "newer", Amount = 2m, Created = new DateTime(2025, 6, 1) }
        ];

        var rows = ReadRows(_sut.CreateExpenditureExcel(items, [], Identity, "UTC"));

        Assert.Equal("newer", rows[1][2]);
        Assert.Equal("older", rows[2][2]);
    }

    // ── CreateFixedIncomeExcel ────────────────────────────────────────────────

    /// <summary>Create fixed income excel localizes headers.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_LocalizesHeaders()
    {
        var rows = ReadRows(_sut.CreateFixedIncomeExcel([], [], Localize, "UTC", 7));

        Assert.Equal("L:MainClass", rows[0][0]);
        Assert.Equal("L:Noticed", rows[0][12]);
        Assert.Equal("L:Expired", rows[0][13]);
    }

    /// <summary>Create fixed income excel localizes class field values.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_LocalizesClassFieldValues()
    {
        FixedIncomeResponse[] items =
        [
            new() { MainClass = "RegularIncome", SubClass = "LaborIncome", Content = "salary", Amount = 100m, MaturityDate = DateTime.UtcNow.AddYears(1) }
        ];

        var row = ReadRows(_sut.CreateFixedIncomeExcel(items, [], Localize, "UTC", 7))[1];

        Assert.Equal("L:RegularIncome", row[0]);
        Assert.Equal("L:LaborIncome", row[1]);
    }

    /// <summary>Create fixed income excel populates monetary unit from deposit asset.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_PopulatesMonetaryUnitFromDepositAsset()
    {
        AssetResponse[] assets = [new() { ProductName = "Bank", MonetaryUnit = "KRW" }];
        FixedIncomeResponse[] items =
        [
            new() { Content = "salary", Amount = 100m, DepositMyAssetProductName = "Bank", MaturityDate = DateTime.UtcNow.AddYears(1) }
        ];

        var row = ReadRows(_sut.CreateFixedIncomeExcel(items, assets, Identity, "UTC", 7))[1];

        Assert.Equal("KRW", row[4]);
    }

    /// <summary>Create fixed income excel expired rows are ordered before active rows.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_ExpiredRowsAreOrderedBeforeActiveRows()
    {
        FixedIncomeResponse[] items =
        [
            new() { Content = "active", Amount = 1m, MaturityDate = new DateTime(2999, 1, 1) },
            new() { Content = "expired", Amount = 2m, MaturityDate = new DateTime(2000, 1, 1) }
        ];

        var rows = ReadRows(_sut.CreateFixedIncomeExcel(items, [], Identity, "UTC", 7));

        Assert.Equal("expired", rows[1][2]);
        Assert.Equal("active", rows[2][2]);
        Assert.Equal("True", rows[1][13]); // Expired column
        Assert.Equal("False", rows[2][13]);
    }

    /// <summary>Create fixed income excel renders maturity date and deposit month day.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_RendersMaturityDateAndDepositMonthDay()
    {
        FixedIncomeResponse[] items =
        [
            new() { Content = "salary", Amount = 1m, DepositMonth = 6, DepositDay = 15, MaturityDate = new DateTime(2030, 12, 31) }
        ];

        var row = ReadRows(_sut.CreateFixedIncomeExcel(items, [], Identity, "UTC", 7))[1];

        Assert.Equal("6", row[5]);
        Assert.Equal("15", row[6]);
        Assert.Equal("2030-12-31", row[7]);
    }

    /// <summary>Create fixed income excel unpunctuality always marks noticed true.</summary>
    [Fact]
    public void CreateFixedIncomeExcel_UnpunctualityAlwaysMarksNoticedTrue()
    {
        FixedIncomeResponse[] items =
        [
            new() { Content = "salary", Amount = 1m, MaturityDate = DateTime.UtcNow.AddYears(1), Unpunctuality = true, DepositMonth = 1, DepositDay = 1 }
        ];

        var row = ReadRows(_sut.CreateFixedIncomeExcel(items, [], Identity, "UTC", 7))[1];

        Assert.Equal("True", row[12]); // Noticed column
    }

    // ── CreateFixedExpenditureExcel ───────────────────────────────────────────

    /// <summary>Create fixed expenditure excel localizes headers and class fields.</summary>
    [Fact]
    public void CreateFixedExpenditureExcel_LocalizesHeadersAndClassFields()
    {
        FixedExpenditureResponse[] items =
        [
            new() { MainClass = "ConsumerSpending", SubClass = "Tax", Content = "insurance", Amount = 50m, MaturityDate = DateTime.UtcNow.AddYears(1) }
        ];

        var rows = ReadRows(_sut.CreateFixedExpenditureExcel(items, [], Localize, "UTC", 7));

        Assert.Equal("L:MainClass", rows[0][0]);
        Assert.Equal("L:ConsumerSpending", rows[1][0]);
        Assert.Equal("L:Tax", rows[1][1]);
    }

    /// <summary>Create fixed expenditure excel trims amount trailing zeros and populates monetary unit.</summary>
    [Fact]
    public void CreateFixedExpenditureExcel_TrimsAmountTrailingZeros_AndPopulatesMonetaryUnit()
    {
        AssetResponse[] assets = [new() { ProductName = "Card", MonetaryUnit = "USD" }];
        FixedExpenditureResponse[] items =
        [
            new() { Content = "insurance", Amount = 250.00m, PaymentMethod = "Card", MaturityDate = DateTime.UtcNow.AddYears(1) }
        ];

        var row = ReadRows(_sut.CreateFixedExpenditureExcel(items, assets, Identity, "UTC", 7))[1];

        Assert.Equal("250", row[3]);
        Assert.Equal("USD", row[4]);
    }

    /// <summary>Create fixed expenditure excel expired rows are ordered before active rows.</summary>
    [Fact]
    public void CreateFixedExpenditureExcel_ExpiredRowsAreOrderedBeforeActiveRows()
    {
        FixedExpenditureResponse[] items =
        [
            new() { Content = "active", Amount = 1m, MaturityDate = new DateTime(2999, 1, 1) },
            new() { Content = "expired", Amount = 2m, MaturityDate = new DateTime(2000, 1, 1) }
        ];

        var rows = ReadRows(_sut.CreateFixedExpenditureExcel(items, [], Identity, "UTC", 7));

        Assert.Equal("expired", rows[1][2]);
        Assert.Equal("active", rows[2][2]);
    }

    // ── CreateAccountExcel ────────────────────────────────────────────────────

    /// <summary>Create account excel localizes headers.</summary>
    [Fact]
    public void CreateAccountExcel_LocalizesHeaders()
    {
        var rows = ReadRows(_sut.CreateAccountExcel([], Localize, "UTC"));

        Assert.Equal("L:Email", rows[0][0]);
        Assert.Equal("L:Nickname", rows[0][1]);
        // Secrets (HashedPassword / RegistrationToken / ResetPasswordToken) are deliberately absent.
        Assert.DoesNotContain("L:HashedPassword", rows[0]);
        Assert.DoesNotContain("L:RegistrationToken", rows[0]);
        Assert.DoesNotContain("L:ResetPasswordToken", rows[0]);
    }

    /// <summary>Create account excel orders by email then nickname ascending.</summary>
    [Fact]
    public void CreateAccountExcel_OrdersByEmailThenNicknameAscending()
    {
        AdminAccountResponse[] items =
        [
            new() { Email = "z@example.com", Nickname = "Z" },
            new() { Email = "a@example.com", Nickname = "A" }
        ];

        var rows = ReadRows(_sut.CreateAccountExcel(items, Identity, "UTC"));

        Assert.Equal("a@example.com", rows[1][0]);
        Assert.Equal("z@example.com", rows[2][0]);
    }

    /// <summary>Create account excel converts created and updated to given time zone.</summary>
    [Fact]
    public void CreateAccountExcel_ConvertsCreatedAndUpdatedToGivenTimeZone()
    {
        var created = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        AdminAccountResponse[] items = [new() { Email = "a@example.com", Created = created, Updated = created }];

        var row = ReadRows(_sut.CreateAccountExcel(items, Identity, "Asia/Seoul"))[1];

        Assert.Equal(created.ConvertTimeByTimeZoneIanaId("Asia/Seoul").ToString(CultureInfo.InvariantCulture), row[9]);
        Assert.Contains("09:00:00", row[9]);
    }

    /// <summary>Create account excel renders boolean and numeric fields as strings.</summary>
    [Fact]
    public void CreateAccountExcel_RendersBooleanAndNumericFieldsAsStrings()
    {
        AdminAccountResponse[] items = [new() { Email = "a@example.com", Locked = true, LoginAttempt = 5, EmailConfirmed = false, AgreedServiceTerms = true, Deleted = true }];

        var row = ReadRows(_sut.CreateAccountExcel(items, Identity, "UTC"))[1];

        Assert.Equal("True", row[5]);  // Locked
        Assert.Equal("5", row[6]);     // LoginAttempt
        Assert.Equal("False", row[7]); // EmailConfirmed
        Assert.Equal("True", row[8]);  // AgreedServiceTerms
        Assert.Equal("True", row[12]); // Deleted
    }

    // ── CreateMenuExcel ───────────────────────────────────────────────────────

    /// <summary>Create menu excel localizes headers.</summary>
    [Fact]
    public void CreateMenuExcel_LocalizesHeaders()
    {
        var rows = ReadRows(_sut.CreateMenuExcel([], [], Localize));

        Assert.Equal(
            ["L:Id", "L:CategoryId", "L:Name", "L:DisplayName", "L:IconPath", "L:Controller", "L:Action", "L:Role", "L:Order"],
            rows[0]);
    }

    /// <summary>Create menu excel category row has empty category id column.</summary>
    [Fact]
    public void CreateMenuExcel_CategoryRow_HasEmptyCategoryIdColumn()
    {
        CategoryResponse[] categories = [new() { Id = 1, Name = "Dashboard", DisplayName = "Dashboard", Controller = "Dashboard", Action = "Index", Role = "User", Order = 1 }];

        var row = ReadRows(_sut.CreateMenuExcel(categories, [], Identity))[1];

        Assert.Equal("1", row[0]);
        Assert.Equal("", row[1]);
        Assert.Equal("Dashboard", row[5]); // Controller column
    }

    /// <summary>Create menu excel sub category row has empty controller column and carries category id.</summary>
    [Fact]
    public void CreateMenuExcel_SubCategoryRow_HasEmptyControllerColumn_AndCarriesCategoryId()
    {
        SubCategoryResponse[] subCategories = [new() { Id = 10, CategoryId = 1, Name = "Sub", DisplayName = "Sub", Action = "Index", Role = "User", Order = 1 }];

        var row = ReadRows(_sut.CreateMenuExcel([], subCategories, Identity))[1];

        Assert.Equal("10", row[0]);
        Assert.Equal("1", row[1]);
        Assert.Equal("", row[5]); // Controller column is blank for sub-categories
    }

    /// <summary>Create menu excel orders combined rows by id descending.</summary>
    [Fact]
    public void CreateMenuExcel_OrdersCombinedRowsByIdDescending()
    {
        CategoryResponse[] categories =
        [
            new() { Id = 1, Name = "Cat1" },
            new() { Id = 2, Name = "Cat2" }
        ];
        SubCategoryResponse[] subCategories = [new() { Id = 10, CategoryId = 1, Name = "Sub10" }];

        var rows = ReadRows(_sut.CreateMenuExcel(categories, subCategories, Identity));

        // Header + 3 data rows, ordered descending by parsed Id: 10, 2, 1
        Assert.Equal("10", rows[1][0]);
        Assert.Equal("2", rows[2][0]);
        Assert.Equal("1", rows[3][0]);
    }
}
