using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Extensions;
using System.Globalization;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Constants;
using Maroik.Website.Models.ViewModels.AccountBooks;

namespace Maroik.Website.Mappers;

/// <summary>
/// Turns the budget DTOs into what the Income Budget / Expenditure Budget page draws: localized row labels, values in the finance grids'
/// amount format (<see cref="AmountDisplay.Format"/>), each budget cell's color (by the sheet's kind: an expenditure over its budget is
/// bad, an income under its budget a warning), and the formula bar's problems localized. Presentation only: the numbers and states
/// come from the domain.
/// </summary>
public static class BudgetViewModelMapper
{
    /// <summary>
    /// The page model: the kind, the account's current year in its time zone, the kind's classes (localized), the asset currencies, and
    /// the captions of the month and year-total columns (localized; which column is which comes from <c>BudgetSheetLayout</c>).
    /// <paramref name="fixedInitialYear"/> is the year to open on instead of the current one, if any.
    /// </summary>
    public static BudgetPageViewModel Page(BudgetSheetKind kind, BudgetPageRules rules, IEnumerable<AssetResponse> assets, DateTime utcNow, string timeZoneIanaId, Func<string, string> localize, int? fixedInitialYear = null) => new()
    {
        Kind = kind.ToString(),
        Year = utcNow.ToTimeZone(timeZoneIanaId).Year,
        InitialYear = fixedInitialYear ?? utcNow.ToTimeZone(timeZoneIanaId).Year,
        MainClasses = rules.MainClasses,
        SubClassesByMainClass = rules.SubClassesByMainClass,
        Functions = rules.Functions,
        ClassLabels = rules.MainClasses.Concat(rules.SubClassesByMainClass.Values.SelectMany(s => s)).Distinct(StringComparer.Ordinal)
            .ToDictionary(c => c, localize, StringComparer.Ordinal),
        Currencies = [.. assets.Select(a => a.MonetaryUnit).OfType<string>().Distinct(StringComparer.Ordinal)],
        ColumnCaptions = rules.MonthByColumn.Select(c => (column: c.Key, key: _monthKeys[c.Value - 1]))
            .Append((column: rules.YearColumn, key: "Year total"))
            .ToDictionary(c => c.column, c => localize(c.key), StringComparer.Ordinal),
    };

    /// <summary>The resx keys of the month columns' captions, January first.</summary>
    private static readonly string[] _monthKeys =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    /// <summary>A value as the grid shows it: a number in the amount format, any other value as its text, empty as nothing.</summary>
    public static string Display(BudgetValueResponse value) =>
        value.Number is { } number ? Amount(number) : value.Text ?? "";

    /// <summary>An amount in the finance grids' format.</summary>
    private static string Amount(decimal amount) => amount.ToString(AmountDisplay.Format, CultureInfo.CurrentCulture);

    /// <summary>A sheet ready to draw.</summary>
    public static BudgetSheetViewModel ToViewModel(this BudgetSheetResponse sheet, Func<string, string> localize) => new()
    {
        Kind = sheet.Kind,
        Year = sheet.Year,
        Exists = sheet.Exists,
        HasLines = sheet.Rows.Any(r => r.Kind == "Line"),
        Rows = [.. sheet.Rows.Select(r => new BudgetRowViewModel
        {
            Number = r.Number,
            Kind = r.Kind,
            Label = r.Kind switch
            {
                "Header" => localize(r.MainClass ?? ""),
                "Line" => localize(r.SubClass ?? ""),
                "Subtotal" => localize("Subtotal"),
                "Total" => localize("Total"),
                _ => "",
            },
            MainClass = r.MainClass ?? "",
            SubClass = r.SubClass ?? "",
            Currency = r.Currency ?? "",
            Editable = r.EditableColumns,
        })],
        Cells = [.. sheet.Cells.Select(c => c.ToViewModel(sheet.Kind))],
        Unbudgeted = [.. sheet.Unbudgeted.Select(u => new BudgetUnbudgetedViewModel
        {
            Label = localize(u.SubClass),
            MainClass = u.MainClass,
            SubClass = u.SubClass,
            Currency = u.Currency,
            Months = [.. u.Months.Select(Amount)],
        })],
    };

    /// <summary>A cell ready to draw; <paramref name="kind"/> decides a budget cell's color.</summary>
    public static BudgetCellViewModel ToViewModel(this BudgetCellResponse cell, string kind) => new()
    {
        Address = cell.Address,
        Role = cell.Role,
        Input = cell.Input ?? "",
        Display = Display(cell.Value),
        ValueKind = cell.Value.Kind,
        Actual = cell.Actual is { } actual ? Amount(actual) : "",
        Remaining = cell.Remaining is { } remaining ? Amount(remaining) : "",
        State = (kind, cell.State) switch
        {
            (_, "Unbudgeted") => "unbudgeted",
            ("Expenditure", "Under") or ("Income", "Reached" or "Over") => "good",
            ("Expenditure", "Reached") or ("Income", "Under") => "warn",
            ("Expenditure", "Over") => "bad",
            _ => "",
        },
    };

    /// <summary>A preview ready to paint, its problems localized by <paramref name="localize"/> (message key, arguments).</summary>
    public static BudgetPreviewViewModel ToViewModel(this BudgetPreviewResponse preview, Func<string, object[], string> localize) => new()
    {
        Tokens = [.. preview.Tokens.Select(t => new BudgetTokenViewModel
            { Start = t.Start, Length = t.Length, Class = t.Class, Reference = t.Reference ?? -1, Partner = t.Partner ?? -1 })],
        References = [.. preview.References.Select(r => new BudgetReferenceViewModel { Index = r.Index, TopLeft = r.TopLeft ?? "", BottomRight = r.BottomRight ?? "" })],
        Problems = [.. preview.Problems.Select(p => new BudgetProblemViewModel { Start = p.Start, Length = p.Length, Message = localize(p.ErrorKey, p.ErrorArgs), Blocking = p.Blocking })],
        CanSave = preview.CanSave,
        Value = preview.Value is null ? "" : Display(preview.Value),
    };
}
