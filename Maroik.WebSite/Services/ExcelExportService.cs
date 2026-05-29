using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Maroik.WebSite.Models.ViewModels.Management;
using Maroik.WebSite.Models.ViewModels.Notice;
using Maroik.WebSite.Utilities;

namespace Maroik.WebSite.Services;

/// <summary>
/// Builds OpenXML Excel workbooks for account-book data export.
/// All localization and timezone conversion is delegated to the caller via <see cref="Func{T,TResult}"/>
/// and a timezone ID string, keeping this class free of presentation-layer concerns.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    /// <inheritdoc />
    public MemoryStream CreateAssetExcel(
        IEnumerable<AssetResponse> items,
        Func<string, string> localize,
        string timeZoneIanaId)
    {
        var ordered = items.OrderBy(x => x.ProductName).ToList();

        var headers = new[]
        {
            localize(nameof(AssetOutputViewModel.ProductName)),
            localize(nameof(AssetOutputViewModel.Item)),
            localize(nameof(AssetOutputViewModel.Amount)),
            localize(nameof(AssetOutputViewModel.MonetaryUnit)),
            localize(nameof(AssetOutputViewModel.Created)),
            localize(nameof(AssetOutputViewModel.Updated)),
            localize(nameof(AssetOutputViewModel.Note)),
            localize(nameof(AssetOutputViewModel.Deleted))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            item.ProductName ?? "",
            localize(item.Item ?? ""),
            item.Amount.TrimTrailingZeros(),
            item.MonetaryUnit ?? "",
            item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(),
            item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(),
            item.Note ?? "",
            item.Deleted.ToString()
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateIncomeExcel(
        IEnumerable<IncomeResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId)
    {
        var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit ?? "");
        var ordered = items.OrderByDescending(x => x.Created).ThenByDescending(x => x.Updated).ToList();

        var headers = new[]
        {
            localize(nameof(IncomeOutputViewModel.MainClass)),
            localize(nameof(IncomeOutputViewModel.SubClass)),
            localize(nameof(IncomeOutputViewModel.Content)),
            localize(nameof(IncomeOutputViewModel.Amount)),
            localize(nameof(IncomeOutputViewModel.MonetaryUnit)),
            localize(nameof(IncomeOutputViewModel.DepositMyAssetProductName)),
            localize(nameof(IncomeOutputViewModel.Created)),
            localize(nameof(IncomeOutputViewModel.Updated)),
            localize(nameof(IncomeOutputViewModel.Note))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            localize(item.MainClass ?? ""),
            localize(item.SubClass ?? ""),
            item.Content ?? "",
            item.Amount.TrimTrailingZeros(),
            currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? "", ""),
            item.DepositMyAssetProductName ?? "",
            item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(),
            item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(),
            item.Note ?? ""
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateExpenditureExcel(
        IEnumerable<ExpenditureResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId)
    {
        var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit ?? "");
        var ordered = items.OrderByDescending(x => x.Created).ThenByDescending(x => x.Updated).ToList();

        var headers = new[]
        {
            localize(nameof(ExpenditureOutputViewModel.MainClass)),
            localize(nameof(ExpenditureOutputViewModel.SubClass)),
            localize(nameof(ExpenditureOutputViewModel.Content)),
            localize(nameof(ExpenditureOutputViewModel.Amount)),
            localize(nameof(ExpenditureOutputViewModel.MonetaryUnit)),
            localize(nameof(ExpenditureOutputViewModel.PaymentMethod)),
            localize(nameof(ExpenditureOutputViewModel.Note)),
            localize(nameof(ExpenditureOutputViewModel.MyDepositAsset)),
            localize(nameof(ExpenditureOutputViewModel.Created)),
            localize(nameof(ExpenditureOutputViewModel.Updated))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            localize(item.MainClass ?? ""),
            localize(item.SubClass ?? ""),
            item.Content ?? "",
            item.Amount.TrimTrailingZeros(),
            currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? "", ""),
            item.PaymentMethod ?? "",
            item.Note ?? "",
            item.MyDepositAsset ?? "",
            item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(),
            item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString()
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateFixedIncomeExcel(
        IEnumerable<FixedIncomeResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId,
        int noticeWindowDays)
    {
        var assetList = assets.ToList();
        var ordered = items
            .ToDisplayViewModels(assetList, localize, timeZoneIanaId, noticeWindowDays)
            .OrderByDescending(a => a.Expired).ThenByDescending(a => a.Noticed)
            .ThenByDescending(a => a.Unpunctuality).ThenByDescending(a => a.Created)
            .ThenByDescending(a => a.Updated).ToList();

        var headers = new[]
        {
            localize(nameof(FixedIncomeOutputViewModel.MainClass)),
            localize(nameof(FixedIncomeOutputViewModel.SubClass)),
            localize(nameof(FixedIncomeOutputViewModel.Content)),
            localize(nameof(FixedIncomeOutputViewModel.Amount)),
            localize(nameof(FixedIncomeOutputViewModel.MonetaryUnit)),
            localize(nameof(FixedIncomeOutputViewModel.DepositMonth)),
            localize(nameof(FixedIncomeOutputViewModel.DepositDay)),
            localize(nameof(FixedIncomeOutputViewModel.MaturityDate)),
            localize(nameof(FixedIncomeOutputViewModel.Note)),
            localize(nameof(FixedIncomeOutputViewModel.DepositMyAssetProductName)),
            localize(nameof(FixedIncomeOutputViewModel.Created)),
            localize(nameof(FixedIncomeOutputViewModel.Updated)),
            localize(nameof(FixedIncomeOutputViewModel.Noticed)),
            localize(nameof(FixedIncomeOutputViewModel.Expired))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            item.MainClass ?? "",
            item.SubClass ?? "",
            item.Content ?? "",
            item.Amount.ToString(),
            item.MonetaryUnit ?? "",
            item.DepositMonth.ToString(),
            item.DepositDay.ToString(),
            item.MaturityDate ?? "",
            item.Note ?? "",
            item.DepositMyAssetProductName ?? "",
            item.Created.ToString(),
            item.Updated.ToString(),
            item.Noticed.ToString(),
            item.Expired.ToString()
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateFixedExpenditureExcel(
        IEnumerable<FixedExpenditureResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId,
        int noticeWindowDays)
    {
        var assetList = assets.ToList();
        var ordered = items
            .ToDisplayViewModels(assetList, localize, timeZoneIanaId, noticeWindowDays)
            .OrderByDescending(a => a.Expired).ThenByDescending(a => a.Noticed)
            .ThenByDescending(a => a.Unpunctuality).ThenByDescending(a => a.Created)
            .ThenByDescending(a => a.Updated).ToList();

        var headers = new[]
        {
            localize(nameof(FixedExpenditureOutputViewModel.MainClass)),
            localize(nameof(FixedExpenditureOutputViewModel.SubClass)),
            localize(nameof(FixedExpenditureOutputViewModel.Content)),
            localize(nameof(FixedExpenditureOutputViewModel.Amount)),
            localize(nameof(FixedExpenditureOutputViewModel.MonetaryUnit)),
            localize(nameof(FixedExpenditureOutputViewModel.PaymentMethod)),
            localize(nameof(FixedExpenditureOutputViewModel.MyDepositAsset)),
            localize(nameof(FixedExpenditureOutputViewModel.DepositMonth)),
            localize(nameof(FixedExpenditureOutputViewModel.DepositDay)),
            localize(nameof(FixedExpenditureOutputViewModel.MaturityDate)),
            localize(nameof(FixedExpenditureOutputViewModel.Created)),
            localize(nameof(FixedExpenditureOutputViewModel.Updated)),
            localize(nameof(FixedExpenditureOutputViewModel.Note)),
            localize(nameof(FixedExpenditureOutputViewModel.Noticed)),
            localize(nameof(FixedExpenditureOutputViewModel.Expired))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            item.MainClass ?? "",
            item.SubClass ?? "",
            item.Content ?? "",
            item.Amount.TrimTrailingZeros(),
            item.MonetaryUnit ?? "",
            item.PaymentMethod ?? "",
            item.MyDepositAsset ?? "",
            item.DepositMonth.ToString(),
            item.DepositDay.ToString(),
            item.MaturityDate ?? "",
            item.Created.ToString(),
            item.Updated.ToString(),
            item.Note ?? "",
            item.Noticed.ToString(),
            item.Expired.ToString()
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateAccountExcel(
        IEnumerable<AccountResponse> items,
        Func<string, string> localize,
        string timeZoneIanaId)
    {
        var ordered = items
            .ToDisplayViewModels(timeZoneIanaId)
            .OrderBy(m => m.Email).ThenBy(m => m.Nickname)
            .ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated)
            .ToList();

        var headers = new[]
        {
            localize(nameof(AccountOutputViewModel.Email)),
            localize(nameof(AccountOutputViewModel.HashedPassword)),
            localize(nameof(AccountOutputViewModel.Nickname)),
            localize(nameof(AccountOutputViewModel.AvatarImagePath)),
            localize(nameof(AccountOutputViewModel.Role)),
            localize(nameof(AccountOutputViewModel.TimeZoneIanaId)),
            localize(nameof(AccountOutputViewModel.Locked)),
            localize(nameof(AccountOutputViewModel.LoginAttempt)),
            localize(nameof(AccountOutputViewModel.EmailConfirmed)),
            localize(nameof(AccountOutputViewModel.AgreedServiceTerms)),
            localize(nameof(AccountOutputViewModel.RegistrationToken)),
            localize(nameof(AccountOutputViewModel.ResetPasswordToken)),
            localize(nameof(AccountOutputViewModel.Created)),
            localize(nameof(AccountOutputViewModel.Updated)),
            localize(nameof(AccountOutputViewModel.Message)),
            localize(nameof(AccountOutputViewModel.Deleted))
        };

        var rows = ordered.Select(item => (IEnumerable<string>)
        [
            item.Email ?? "",
            item.HashedPassword ?? "",
            item.Nickname ?? "",
            item.AvatarImagePath ?? "",
            item.Role ?? "",
            item.TimeZoneIanaId ?? "",
            item.Locked.ToString(),
            item.LoginAttempt.ToString(),
            item.EmailConfirmed.ToString(),
            item.AgreedServiceTerms.ToString(),
            item.RegistrationToken ?? "",
            item.ResetPasswordToken ?? "",
            item.Created.ToString(),
            item.Updated.ToString(),
            item.Message ?? "",
            item.Deleted.ToString()
        ]);

        return BuildExcel(headers, rows);
    }

    /// <inheritdoc />
    public MemoryStream CreateMenuExcel(
        IEnumerable<CategoryResponse> categories,
        IEnumerable<SubCategoryResponse> subCategories,
        Func<string, string> localize)
    {
        var rows = categories
            .Select(c => (IEnumerable<string>)
            [
                c.Id.ToString(), "", c.Name ?? "", c.DisplayName ?? "",
                c.IconPath ?? "", c.Controller ?? "", c.Action ?? "", c.Role ?? "", c.Order.ToString()
            ])
            .Concat(subCategories.Select(s => (IEnumerable<string>)
            [
                s.Id.ToString(), s.CategoryId.ToString(), s.Name ?? "", s.DisplayName ?? "",
                s.IconPath ?? "", "", s.Action ?? "", s.Role ?? "", s.Order.ToString()
            ]))
            .OrderByDescending(r => long.Parse(r.First()));

        var headers = new[]
        {
            localize(nameof(MenuOutputViewModel.Id)),
            localize(nameof(MenuOutputViewModel.CategoryId)),
            localize(nameof(MenuOutputViewModel.Name)),
            localize(nameof(MenuOutputViewModel.DisplayName)),
            localize(nameof(MenuOutputViewModel.IconPath)),
            localize(nameof(MenuOutputViewModel.Controller)),
            localize(nameof(MenuOutputViewModel.Action)),
            localize(nameof(MenuOutputViewModel.Role)),
            localize(nameof(MenuOutputViewModel.Order))
        };

        return BuildExcel(headers, rows);
    }

    private static MemoryStream BuildExcel(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            });

            var headerRow = new Row();
            foreach (string header in headers)
                headerRow.Append(StringCell(header));
            sheetData.Append(headerRow);

            foreach (var rowValues in rows)
            {
                var dataRow = new Row();
                foreach (string value in rowValues)
                    dataRow.Append(StringCell(value));
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static Cell StringCell(string value) => new()
    {
        CellValue = new CellValue(value),
        DataType = CellValues.String
    };
}
