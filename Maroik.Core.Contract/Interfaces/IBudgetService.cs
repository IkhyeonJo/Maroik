using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Use cases of the budget sheets (Income Budget / Expenditure Budget): show a sheet, preview an input being typed, edit cells,
/// paste / fill, add and remove lines, and carry last year's budgets over. Everything is scoped to the signed-in account; months are
/// taken in its time zone.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// The year the budget pages first open on for this account: the fixed demo year for the public demo account, or null for
    /// everyone else (and when that year is not a valid one), whose pages open on the current year.
    /// </summary>
    int? GetFixedInitialYear(string accountEmail);

    /// <summary>The account's sheet of a kind and year (a sheet not stored yet is shown with its default lines).</summary>
    Task<ServiceValueResult<BudgetSheetResponse>> GetSheetAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, CancellationToken ct = default);

    /// <summary>The coloring of an input being typed into a cell and the value it would have; nothing is stored.</summary>
    Task<ServiceValueResult<BudgetPreviewResponse>> PreviewAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, string address, string input, CancellationToken ct = default);

    /// <summary>Stores typed edits of cells and returns the cells whose value changed.</summary>
    Task<ServiceValueResult<BudgetEditResponse>> EditAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, IReadOnlyList<BudgetCellEditRequest> edits, CancellationToken ct = default);

    /// <summary>Pastes (or fills) <paramref name="source"/> over <paramref name="target"/>, as Excel does.</summary>
    Task<ServiceValueResult<BudgetEditResponse>> CopyAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, string source, string target, CancellationToken ct = default);

    /// <summary>Adds a line (a class in a currency) and returns the sheet.</summary>
    Task<ServiceValueResult<BudgetSheetResponse>> AddLineAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, string? mainClass, string? subClass, string? currency, CancellationToken ct = default);

    /// <summary>Removes a line and its inputs and returns the sheet.</summary>
    Task<ServiceValueResult<BudgetSheetResponse>> RemoveLineAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, string? mainClass, string? subClass, string? currency, CancellationToken ct = default);

    /// <summary>Fills the sheet's empty budget cells from last year's sheet and returns the sheet and how many cells were filled.</summary>
    Task<ServiceValueResult<BudgetCarryOverResponse>> CarryOverAsync(string accountEmail, BudgetSheetKind kind, int year, string timeZoneIanaId, CancellationToken ct = default);
}
