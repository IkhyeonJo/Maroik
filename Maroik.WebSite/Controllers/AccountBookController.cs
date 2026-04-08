using System.Globalization;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages account-book records: assets, income transactions, and expenditure transactions
/// (create, read, update, delete, export).
/// </summary>
public class AccountBookController(
    IHtmlLocalizer<AccountBookController> localizer,
    IAssetService assetService,
    IIncomeService incomeService,
    IExpenditureService expenditureService,
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    private readonly ISessionService _sessionService = sessionService;

    #region Asset

    #region Create

    /// <summary>Creates a new asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new AssetRequest
        {
            ProductName = assetInputViewModel.ProductName,
            Item = assetInputViewModel.Item,
            Amount = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note = assetInputViewModel.Note ?? "",
            Deleted = false
        };

        var result = await assetService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The asset has been successfully created."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the asset grid (AJAX) or the full asset page.</summary>
    [HttpGet]
    public async Task<IActionResult> Asset(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            List<AssetOutputViewModel> assetOutputViewModels = [];

            #region Assets

            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            foreach (var item in await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    assetOutputViewModels.Add(new AssetOutputViewModel()
                    {
                        ProductName = item.ProductName,
                        Item = localizer[item.Item ?? string.Empty].Value,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = item.MonetaryUnit,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty),
                        Note = item.Note,
                        Deleted = item.Deleted
                    });
                }
                else
                {
                    if ((item.ProductName ?? "").Contains(wholeSearch) ||
                        localizer[item.Item ?? ""].Value.Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros()).Contains(wholeSearch) ||
                        (item.MonetaryUnit ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty).ToString(CultureInfo.InvariantCulture))
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty).ToString(CultureInfo.InvariantCulture))
                        .Contains(wholeSearch) ||
                        (item.Note ?? "").Contains(wholeSearch) ||
                        (item.Deleted.ToString()).Contains(wholeSearch))
                    {
                        assetOutputViewModels.Add(new AssetOutputViewModel()
                        {
                            ProductName = item.ProductName,
                            Item = localizer[item.Item ?? string.Empty].Value,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = item.MonetaryUnit,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId ?? string.Empty),
                            Note = item.Note,
                            Deleted = item.Deleted
                        });
                    }
                }
            }

            #endregion

            var result =
                assetOutputViewModels.OrderBy(m => m.ProductName).AsQueryable();
            return PartialView("_AssetGrid", result);
        }

        {
            var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            var tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            var assets = (await assetService.GetAssetsAsync(loginedAccount.Email ?? string.Empty))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    /// <summary>Checks whether an asset with the given product name exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsAssetExists(string productName)
    {
        try
        {
            var tempAsset = await assetService.GetAssetAsync(
                _sessionService.GetAccount()!.Email ?? string.Empty, productName);

            return tempAsset == null
                ? Json(new
                {
                    result = false, error = localizer["Fail to find the asset by given product name"].Value
                })
                : (IActionResult)Json(new { result = true, asset = tempAsset });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        _ = ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new AssetRequest
        {
            ProductName = assetInputViewModel.ProductName,
            Item = assetInputViewModel.Item,
            Amount = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note = assetInputViewModel.Note,
            Deleted = assetInputViewModel.Deleted
        };

        var result = await assetService.UpdateAsync(email, request, assetInputViewModel.OriginalProductName);
        return result.Success
            ? Json(new { result = true, message = localizer["The asset has been successfully updated."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    /// <summary>Soft-deletes an existing asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var result = await assetService.DeleteAsync(email, assetInputViewModel.ProductName);
        return result.Success
            ? Json(new { result = true, message = localizer["The asset has been successfully deleted."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Excel

    /// <summary>Exports asset records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelAsset(string fileName = "")
    {
        await Task.Yield();

        var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;

        var assetOutputViewModels = (await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).Select(item => new AssetOutputViewModel
            {
                ProductName = item.ProductName,
                Item = item.Item,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = item.MonetaryUnit,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note,
                Deleted = item.Deleted
            })
            .ToList();

        assetOutputViewModels = assetOutputViewModels.OrderBy(m => m.ProductName).ToList();

        var stream = new MemoryStream();
        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var worksheet = new Worksheet();
            var sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(AssetOutputViewModel.ProductName)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Item)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Amount)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.MonetaryUnit)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Updated)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Note)].Value),
                CreateCell(localizer[nameof(AssetOutputViewModel.Deleted)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in assetOutputViewModels)
            {
                var dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.ProductName),
                    CreateCell(localizer[item.Item ?? ""].Value),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Note),
                    CreateCell(item.Deleted.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Income

    #region Create

    /// <summary>Creates a new income transaction.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new IncomeRequest
        {
            MainClass = incomeInputViewModel.MainClass,
            SubClass = incomeInputViewModel.SubClass,
            Content = incomeInputViewModel.Content ?? "",
            Amount = incomeInputViewModel.Amount,
            DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
            Created = incomeInputViewModel.Created,
            Note = incomeInputViewModel.Note ?? ""
        };

        var result = await incomeService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The income has been successfully created."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the income grid (AJAX) or the full income page.</summary>
    [HttpGet]
    public async Task<IActionResult> Income(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            List<IncomeOutputViewModel> incomeOutputViewModels = [];

            #region Incomes

            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;

            var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            foreach (var item in await incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email ?? string.Empty))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    incomeOutputViewModels.Add(new IncomeOutputViewModel
                    {
                        Id = item.Id,
                        MainClass = localizer[item.MainClass ?? string.Empty].Value,
                        SubClass = localizer[item.SubClass ?? string.Empty].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets
                            .FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                            ?.MonetaryUnit,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note
                    });
                }
                else
                {
                    if (localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) ||
                        localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros()).Contains(wholeSearch) ||
                        (assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                            ?.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMyAssetProductName ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture))
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture))
                        .Contains(wholeSearch) ||
                        (item.Note ?? "").Contains(wholeSearch))
                    {
                        incomeOutputViewModels.Add(new IncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass ?? string.Empty].Value,
                            SubClass = localizer[item.SubClass ?? string.Empty].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets
                                .FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                                ?.MonetaryUnit,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Note = item.Note
                        });
                    }
                }
            }

            #endregion

            IQueryable<IncomeOutputViewModel> result = incomeOutputViewModels.OrderByDescending(m => m.Created)
                .ThenByDescending(m => m.Updated).AsQueryable();
            return PartialView("_IncomeGrid", result);
        }

        {
            var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            var tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            var assets = (await assetService.GetAssetsAsync(loginedAccount.Email ?? string.Empty))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    /// <summary>Checks whether an income record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsIncomeExists(int id)
    {
        try
        {
            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;
            var tempIncomes = await incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            {
                var tempIncome = tempIncomes.FirstOrDefault(a => a.Id == id);

                if (tempIncome == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                tempIncome.Created =
                    tempIncome.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                return Json(new { result = true, income = tempIncome });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given income product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetIncomeAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            var asset =
                (await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = localizer["Amount"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new IncomeRequest
        {
            Id = incomeInputViewModel.Id,
            MainClass = incomeInputViewModel.MainClass,
            SubClass = incomeInputViewModel.SubClass,
            Content = incomeInputViewModel.Content,
            Amount = incomeInputViewModel.Amount,
            DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
            Created = incomeInputViewModel.Created,
            Note = incomeInputViewModel.Note
        };

        var result = await incomeService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The income has been successfully updated."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    /// <summary>Deletes an existing income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var result = await incomeService.DeleteAsync(email, incomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = localizer["The income has been successfully deleted."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Excel

    /// <summary>Exports income records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelIncome(string fileName = "")
    {
        await Task.Yield();

        #region Incomes

        var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;

        var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

        var incomeOutputViewModels = (await incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).Select(item => new IncomeOutputViewModel
            {
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets
                    .FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                    ?.MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note
            })
            .ToList();

        incomeOutputViewModels = incomeOutputViewModels.OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var worksheet = new Worksheet();
            var sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(IncomeOutputViewModel.MainClass)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.SubClass)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.Content)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.Amount)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.MonetaryUnit)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.DepositMyAssetProductName)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.Updated)].Value),
                CreateCell(localizer[nameof(IncomeOutputViewModel.Note)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in incomeOutputViewModels)
            {
                var dataRow = new Row();
                dataRow.Append(
                    CreateCell(localizer[item?.MainClass ?? ""].Value),
                    CreateCell(localizer[item?.SubClass ?? ""].Value),
                    CreateCell(item?.Content),
                    CreateCell(Convert.ToDecimal(item?.Amount.TrimTrailingZeros()).ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.MonetaryUnit),
                    CreateCell(item?.DepositMyAssetProductName),
                    CreateCell(item?.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Note)
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Expenditure

    /// <summary>Creates a new expenditure transaction.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new ExpenditureRequest
        {
            MainClass = expenditureInputViewModel.MainClass,
            SubClass = expenditureInputViewModel.SubClass,
            Content = expenditureInputViewModel.Content ?? "",
            Amount = expenditureInputViewModel.Amount,
            PaymentMethod = expenditureInputViewModel.PaymentMethod,
            MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
            Created = expenditureInputViewModel.Created,
            Note = expenditureInputViewModel.Note ?? ""
        };

        var result = await expenditureService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #region Read

    /// <summary>Returns the expenditure grid (AJAX) or the full expenditure page.</summary>
    [HttpGet]
    public async Task<IActionResult> Expenditure(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            List<ExpenditureOutputViewModel> expenditureOutputViewModels = [];

            #region Expenditures

            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;

            var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            expenditureOutputViewModels.AddRange(from item in await expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email ?? string.Empty)
                where string.IsNullOrEmpty(wholeSearch) || localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) || localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) || (item.Content ?? "").Contains(wholeSearch) || (item.Amount.TrimTrailingZeros()).Contains(wholeSearch) || (assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)
                    ?.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) || (item.PaymentMethod ?? "").Contains(wholeSearch) || (item.Note ?? "").Contains(wholeSearch) || (item.MyDepositAsset ?? "").Contains(wholeSearch) || (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture)).Contains(wholeSearch) || (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture)).Contains(wholeSearch)
                select new ExpenditureOutputViewModel
                {
                    Id = item.Id,
                    MainClass = localizer[item.MainClass ?? string.Empty].Value,
                    SubClass = localizer[item.SubClass ?? string.Empty].Value,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)
                        ?.MonetaryUnit,
                    PaymentMethod = item.PaymentMethod,
                    Note = item.Note,
                    MyDepositAsset = item.MyDepositAsset,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                });

            #endregion

            var result = expenditureOutputViewModels
                .OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
            return PartialView("_ExpenditureGrid", result);
        }

        {
            var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            var tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            var assets = (await assetService.GetAssetsAsync(loginedAccount.Email ?? string.Empty))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    /// <summary>Checks whether an expenditure record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsExpenditureExists(int id)
    {
        try
        {
            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;
            var tempExpenditures =
                await expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            {
                var tempExpenditure = tempExpenditures.FirstOrDefault(a => a.Id == id);

                if (tempExpenditure == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                tempExpenditure.Created =
                    tempExpenditure.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                return Json(new { result = true, expenditure = tempExpenditure });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given expenditure product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetExpenditureAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            var asset =
                (await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = localizer["Amount"].Value });
        }
    }

    #endregion

    /// <summary>Updates an existing expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var request = new ExpenditureRequest
        {
            Id = expenditureInputViewModel.Id,
            MainClass = expenditureInputViewModel.MainClass,
            SubClass = expenditureInputViewModel.SubClass,
            Content = expenditureInputViewModel.Content ?? "",
            Amount = expenditureInputViewModel.Amount,
            PaymentMethod = expenditureInputViewModel.PaymentMethod,
            MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
            Created = expenditureInputViewModel.Created,
            Note = expenditureInputViewModel.Note ?? ""
        };

        var result = await expenditureService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    /// <summary>Deletes an existing expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        var email = _sessionService.GetAccount()!.Email ?? string.Empty;

        var result = await expenditureService.DeleteAsync(email, expenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = localizer["The expenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #region Excel

    /// <summary>Exports expenditure records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
    {
        await Task.Yield();

        #region Expenditures

        var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId ?? string.Empty;

        var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

        var expenditureOutputViewModels = (await expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).Select(item => new ExpenditureOutputViewModel
            {
                MainClass = localizer[item?.MainClass ?? string.Empty].Value,
                SubClass = localizer[item?.SubClass ?? string.Empty].Value,
                Content = item?.Content,
                Amount = Convert.ToDecimal(item?.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item?.PaymentMethod)?.MonetaryUnit,
                PaymentMethod = item?.PaymentMethod,
                Note = item?.Note,
                MyDepositAsset = item?.MyDepositAsset,
                Created = item!.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item!.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
            })
            .ToList();

        expenditureOutputViewModels = expenditureOutputViewModels.OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var worksheet = new Worksheet();
            var sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.MainClass)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.SubClass)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.Content)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.Amount)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.MonetaryUnit)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.PaymentMethod)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.Note)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.MyDepositAsset)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(ExpenditureOutputViewModel.Updated)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in expenditureOutputViewModels)
            {
                var dataRow = new Row();
                dataRow.Append(
                    CreateCell(localizer[item?.MainClass ?? ""].Value),
                    CreateCell(localizer[item?.SubClass ?? ""].Value),
                    CreateCell(item?.Content),
                    CreateCell(Convert.ToDecimal(item?.Amount.TrimTrailingZeros()).ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.MonetaryUnit),
                    CreateCell(item?.PaymentMethod),
                    CreateCell(item?.Note),
                    CreateCell(item?.MyDepositAsset),
                    CreateCell(item?.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Updated.ToString(CultureInfo.InvariantCulture))
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion
}
