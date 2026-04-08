using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>Manages fixed-income and fixed-expenditure notice records (create, read, update, delete, export).</summary>
public class NoticeController(
    IHtmlLocalizer<NoticeController> localizer,
    IAssetService assetService,
    IFixedIncomeService fixedIncomeService,
    IFixedExpenditureService fixedExpenditureService,
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> settings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, settings, sessionService)
{
    private readonly IOptions<ServerSetting> _settings = settings;
    private readonly ISessionService _sessionService = sessionService;

    #region FixedIncome

    #region Create

    /// <summary>Creates a new fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email;

        var request = new FixedIncomeRequest
        {
            Id = fixedIncomeInputViewModel.Id,
            MainClass = fixedIncomeInputViewModel.MainClass,
            SubClass = fixedIncomeInputViewModel.SubClass,
            Content = fixedIncomeInputViewModel.Content ?? "",
            Amount = fixedIncomeInputViewModel.Amount,
            DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
            DepositMonth = fixedIncomeInputViewModel.DepositMonth,
            DepositDay = fixedIncomeInputViewModel.DepositDay,
            MaturityDate = ParseMaturityDate(fixedIncomeInputViewModel.MaturityDate),
            Note = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await fixedIncomeService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedIncome has been successfully created."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the fixed-income grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedIncome(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            List<FixedIncomeOutputViewModel> fixedIncomeOutputViewModels = [];

            #region FixedIncomes

            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            var currentDate =
                new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

            foreach (var item in await fixedIncomeService.GetFixedIncomesAsync(_sessionService.GetAccount()!.Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    var noticedResult = false;

                    try
                    {
                        noticedResult =
                            currentDate.Subtract(new DateTime(
                                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"),
                                    "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(
                                currentDate.Subtract(new DateTime(
                                    Convert.ToInt32(
                                        Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                                    item.DepositMonth, item.DepositDay)).TotalDays) <=
                            _settings.Value.NoticeMaturityDateDay;
                    }
                    catch // Exception occurs when initialized to February 29th in a non-leap year
                    {
                        noticedResult = false;
                    }

                    if (item.Unpunctuality) // Set notification to appear when unpunctuality is checked
                    {
                        noticedResult = true;
                    }

                    fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel
                    {
                        Id = item.Id,
                        MainClass = localizer[item.MainClass.ToString()].Value,
                        SubClass = localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                            ?.MonetaryUnit,
                        DepositMonth = item.DepositMonth,
                        DepositDay = item.DepositDay,
                        MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                        Note = item.Note,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Noticed = noticedResult,
                        Unpunctuality = item.Unpunctuality,
                        Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                    });
                }
                else
                {
                    if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                            ?.MonetaryUnit ?? "").Contains(wholeSearch) ||
                        (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch))
                    {
                        var noticedResult = false;

                        try
                        {
                            noticedResult =
                                currentDate.Subtract(new DateTime(
                                    Convert.ToInt32(
                                        Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                                    item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate
                                    .Subtract(new DateTime(
                                        Convert.ToInt32(
                                            Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")
                                                [0]), item.DepositMonth, item.DepositDay)).TotalDays) <=
                                _settings.Value.NoticeMaturityDateDay;
                        }
                        catch // Exception occurs when initialized to February 29th in a non-leap year
                        {
                            noticedResult = false;
                        }

                        if (item.Unpunctuality) // Set notification to appear when unpunctuality is checked
                        {
                            noticedResult = true;
                        }

                        fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                                ?.MonetaryUnit,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Note = item.Note,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                }
            }

            #endregion

            var result = fixedIncomeOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedIncomeGrid", result);
        }

        {
            var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            var assets = (await assetService.GetAssetsAsync(loginedAccount.Email))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            ViewBag.Assets = assets;
        }
        return View();
    }

    /// <summary>Checks whether a fixed-income record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsFixedIncomeExists(int id)
    {
        try
        {
            var tempFixedIncomes =
                await fixedIncomeService.GetFixedIncomesAsync(_sessionService.GetAccount()!.Email);

            {
                var tempFixedIncome = tempFixedIncomes.FirstOrDefault(a => a.Id == id);

                if (tempFixedIncome == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                var tempFixedIncomeOutputModel = new FixedIncomeOutputViewModel
                {
                    Id = tempFixedIncome.Id,
                    MainClass = tempFixedIncome.MainClass,
                    SubClass = tempFixedIncome.SubClass,
                    Content = tempFixedIncome.Content,
                    Amount = Convert.ToDecimal(tempFixedIncome.Amount.TrimTrailingZeros()),
                    DepositMonth = tempFixedIncome.DepositMonth,
                    DepositDay = tempFixedIncome.DepositDay,
                    MaturityDate = tempFixedIncome.MaturityDate.ToString("yyyy-MM-dd"),
                    Note = tempFixedIncome.Note,
                    DepositMyAssetProductName = tempFixedIncome.DepositMyAssetProductName,
                    Unpunctuality = tempFixedIncome.Unpunctuality
                };
                return Json(new { result = true, fixedIncome = tempFixedIncomeOutputModel });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given fixed-income product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetFixedIncomeAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            var asset =
                (await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
                .Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new AssetResponse();

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

    /// <summary>Updates an existing fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email;

        var request = new FixedIncomeRequest
        {
            Id = fixedIncomeInputViewModel.Id,
            MainClass = fixedIncomeInputViewModel.MainClass,
            SubClass = fixedIncomeInputViewModel.SubClass,
            Content = fixedIncomeInputViewModel.Content ?? "",
            Amount = fixedIncomeInputViewModel.Amount,
            DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
            DepositMonth = fixedIncomeInputViewModel.DepositMonth,
            DepositDay = fixedIncomeInputViewModel.DepositDay,
            MaturityDate = ParseMaturityDate(fixedIncomeInputViewModel.MaturityDate),
            Note = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await fixedIncomeService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedIncome has been successfully updated."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    /// <summary>Deletes an existing fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        var email = _sessionService.GetAccount()!.Email;

        var result = await fixedIncomeService.DeleteAsync(email, fixedIncomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedIncome has been successfully deleted."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #region Excel

    /// <summary>Exports fixed-income records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelFixedIncome(string fileName = "")
    {
        await Task.Yield();

        var account = _sessionService.GetAccount() ?? new AccountResponse();
        var loginedAccountTimeZoneIanaId = account.TimeZoneIanaId;
        var currentDate = DateTime.UtcNow.ToLocalTime();

        var assets = await assetService.GetAssetsAsync(account.Email);
        var fixedIncomeList = await fixedIncomeService.GetFixedIncomesAsync(account.Email);

        var fixedIncomeOutputViewModels = fixedIncomeList.Select(item =>
            {
                var noticedResult = false;

                try
                {
                    var maturityDate = new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay);
                    noticedResult = currentDate.Subtract(maturityDate).TotalDays <= 0 &&
                                    Math.Abs(currentDate.Subtract(maturityDate).TotalDays) <=
                                    _settings.Value.NoticeMaturityDateDay;
                }
                catch
                {
                    noticedResult = false;
                }

                if (item.Unpunctuality)
                {
                    noticedResult = true;
                }

                var asset = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName);
                return new FixedIncomeOutputViewModel
                {
                    MainClass = localizer[item.MainClass.ToString()].Value,
                    SubClass = localizer[item.SubClass.ToString()].Value,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = asset?.MonetaryUnit,
                    DepositMonth = item.DepositMonth,
                    DepositDay = item.DepositDay,
                    MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                    Note = item.Note,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Noticed = noticedResult,
                    Unpunctuality = item.Unpunctuality,
                    Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                };
            }).OrderByDescending(a => a.Expired)
            .ThenByDescending(m => m.Noticed)
            .ThenByDescending(m => m.Unpunctuality)
            .ThenByDescending(a => a.Created)
            .ThenByDescending(a => a.Updated)
            .ToList();

        var stream = new MemoryStream();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            Cell CreateCell(string text)
            {
                return new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(text)
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.MainClass)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.SubClass)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Content)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Amount)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.MonetaryUnit)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.DepositMonth)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.DepositDay)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.MaturityDate)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Note)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.DepositMyAssetProductName)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Updated)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Noticed)].Value),
                CreateCell(localizer[nameof(FixedIncomeOutputViewModel.Expired)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in fixedIncomeOutputViewModels)
            {
                var dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.MainClass),
                    CreateCell(item.SubClass),
                    CreateCell(item.Content),
                    CreateCell(item.Amount.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.DepositMonth.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.DepositDay.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.MaturityDate),
                    CreateCell(item.Note),
                    CreateCell(item.DepositMyAssetProductName),
                    CreateCell(item.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Noticed.ToString()),
                    CreateCell(item.Expired.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
    }

    #endregion

    #endregion

    #endregion

    #region FixedExpenditure

    #region Create

    /// <summary>Creates a new fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email;

        var request = new FixedExpenditureRequest
        {
            Id = fixedExpenditureInputViewModel.Id,
            MainClass = fixedExpenditureInputViewModel.MainClass,
            SubClass = fixedExpenditureInputViewModel.SubClass,
            Content = fixedExpenditureInputViewModel.Content ?? "",
            Amount = fixedExpenditureInputViewModel.Amount,
            PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
            MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "",
            DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
            DepositDay = fixedExpenditureInputViewModel.DepositDay,
            MaturityDate = ParseMaturityDate(fixedExpenditureInputViewModel.MaturityDate),
            Note = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await fixedExpenditureService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedExpenditure has been successfully created."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the fixed-expenditure grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedExpenditure(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels = [];

            #region FixedExpenditures

            var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            var currentDate =
                new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

            foreach (var item in await fixedExpenditureService.GetFixedExpendituresAsync(
                         _sessionService.GetAccount()!.Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    var noticedResult = false;

                    try
                    {
                        noticedResult =
                            currentDate.Subtract(new DateTime(
                                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"),
                                    "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(
                                currentDate.Subtract(new DateTime(
                                    Convert.ToInt32(
                                        Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                                    item.DepositMonth, item.DepositDay)).TotalDays) <=
                            _settings.Value.NoticeMaturityDateDay;
                    }
                    catch // Exception occurs when initialized to February 29th in a non-leap year
                    {
                        noticedResult = false;
                    }

                    if (item.Unpunctuality) // Set notification to appear when unpunctuality is checked
                    {
                        noticedResult = true;
                    }

                    fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel
                    {
                        Id = item.Id,
                        MainClass = localizer[item.MainClass.ToString()].Value,
                        SubClass = localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)
                            ?.MonetaryUnit,
                        PaymentMethod = item.PaymentMethod,
                        MyDepositAsset = item.MyDepositAsset,
                        DepositMonth = item.DepositMonth,
                        DepositDay = item.DepositDay,
                        MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note,
                        Noticed = noticedResult,
                        Unpunctuality = item.Unpunctuality,
                        Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                    });
                }
                else
                {
                    if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit ?? "")
                        .Contains(wholeSearch) ||
                        (item.PaymentMethod.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch))
                    {
                        var noticedResult = false;

                        try
                        {
                            noticedResult =
                                currentDate.Subtract(new DateTime(
                                    Convert.ToInt32(
                                        Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                                    item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate
                                    .Subtract(new DateTime(
                                        Convert.ToInt32(
                                            Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")
                                                [0]), item.DepositMonth, item.DepositDay)).TotalDays) <=
                                _settings.Value.NoticeMaturityDateDay;
                        }
                        catch // Exception occurs when initialized to February 29th in a non-leap year
                        {
                            noticedResult = false;
                        }

                        if (item.Unpunctuality) // Set notification to appear when unpunctuality is checked
                        {
                            noticedResult = true;
                        }

                        fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)
                                ?.MonetaryUnit,
                            PaymentMethod = item.PaymentMethod,
                            MyDepositAsset = item.MyDepositAsset,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Note = item.Note,
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                }
            }

            #endregion

            var result = fixedExpenditureOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedExpenditureGrid", result);
        }

        {
            var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            var assets = (await assetService.GetAssetsAsync(loginedAccount.Email))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            ViewBag.Assets = assets;
        }
        return View();
    }

    /// <summary>Checks whether a fixed-expenditure record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsFixedExpenditureExists(int id)
    {
        try
        {
            var tempFixedExpenditures =
                await fixedExpenditureService.GetFixedExpendituresAsync(_sessionService.GetAccount()!.Email);

            {
                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == id);

                if (tempFixedExpenditure == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                var tempFixedIncomeOutputModel = new FixedExpenditureOutputViewModel
                {
                    Id = tempFixedExpenditure.Id,
                    MainClass = tempFixedExpenditure.MainClass,
                    SubClass = tempFixedExpenditure.SubClass,
                    Content = tempFixedExpenditure.Content,
                    Amount = Convert.ToDecimal(tempFixedExpenditure.Amount.TrimTrailingZeros()),
                    DepositMonth = tempFixedExpenditure.DepositMonth,
                    DepositDay = tempFixedExpenditure.DepositDay,
                    MaturityDate = tempFixedExpenditure.MaturityDate.ToString("yyyy-MM-dd"),
                    Note = tempFixedExpenditure.Note,
                    PaymentMethod = tempFixedExpenditure.PaymentMethod,
                    MyDepositAsset = tempFixedExpenditure.MyDepositAsset ?? "",
                    Unpunctuality = tempFixedExpenditure.Unpunctuality
                };
                return Json(new { result = true, fixedExpenditure = tempFixedIncomeOutputModel });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given fixed-expenditure product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetFixedExpenditureAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            var asset =
                (await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
                .Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new AssetResponse();

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

    /// <summary>Updates an existing fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = localizer["Input is invalid"].Value });

        var email = _sessionService.GetAccount()!.Email;

        var request = new FixedExpenditureRequest
        {
            Id = fixedExpenditureInputViewModel.Id,
            MainClass = fixedExpenditureInputViewModel.MainClass,
            SubClass = fixedExpenditureInputViewModel.SubClass,
            Content = fixedExpenditureInputViewModel.Content ?? "",
            Amount = fixedExpenditureInputViewModel.Amount,
            PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
            MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "",
            DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
            DepositDay = fixedExpenditureInputViewModel.DepositDay,
            MaturityDate = ParseMaturityDate(fixedExpenditureInputViewModel.MaturityDate),
            Note = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await fixedExpenditureService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedExpenditure has been successfully updated."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    /// <summary>Deletes an existing fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        var email = _sessionService.GetAccount()!.Email;

        var result = await fixedExpenditureService.DeleteAsync(email, fixedExpenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = localizer["The fixedExpenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = localizer[result.ErrorKey].Value });
    }

    #region Excel

    /// <summary>Exports fixed-expenditure records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelFixedExpenditure(string fileName = "")
    {
        await Task.Yield();

        #region FixedExpenditures

        var loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

        var currentDate =
            new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

        var assets = await assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

        var fixedExpenditureOutputViewModels = new List<FixedExpenditureOutputViewModel>();

        foreach (var item in await fixedExpenditureService.GetFixedExpendituresAsync(_sessionService.GetAccount()!.Email))
        {
            var noticedResult = false;

            try
            {
                noticedResult =
                    currentDate.Subtract(new DateTime(
                        Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                        item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate
                        .Subtract(new DateTime(
                            Convert.ToInt32(
                                Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                            item.DepositMonth, item.DepositDay)).TotalDays) <= _settings.Value.NoticeMaturityDateDay;
            }
            catch // Exception occurs when initialized to February 29th in a non-leap year
            {
                noticedResult = false;
            }

            if (item.Unpunctuality) // Set to show notification when unpunctuality is checked
            {
                noticedResult = true;
            }

            fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel
            {
                MainClass = localizer[item.MainClass.ToString()].Value,
                SubClass = localizer[item.SubClass.ToString()].Value,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                PaymentMethod = item.PaymentMethod,
                MyDepositAsset = item.MyDepositAsset,
                DepositMonth = item.DepositMonth,
                DepositDay = item.DepositDay,
                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note,
                Noticed = noticedResult,
                Unpunctuality = item.Unpunctuality,
                Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
            });
        }

        fixedExpenditureOutputViewModels = fixedExpenditureOutputViewModels.OrderByDescending(a => a.Expired)
            .ThenByDescending(m => m.Noticed).ThenByDescending(m => m.Unpunctuality)
            .ThenByDescending(a => a.Created).ThenByDescending(a => a.Updated).ToList();

        #endregion

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

            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.MainClass)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.SubClass)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Content)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Amount)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.MonetaryUnit)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.PaymentMethod)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.MyDepositAsset)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.DepositMonth)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.DepositDay)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.MaturityDate)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Updated)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Note)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Noticed)].Value),
                CreateCell(localizer[nameof(FixedExpenditureOutputViewModel.Expired)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in fixedExpenditureOutputViewModels)
            {
                var dataRow = new Row();
                dataRow.Append(
                    CreateCell(localizer[item?.MainClass ?? ""].Value),
                    CreateCell(localizer[item?.SubClass ?? ""].Value),
                    CreateCell(item?.Content),
                    CreateCell(Convert.ToDecimal(item?.Amount.TrimTrailingZeros()).ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.MonetaryUnit),
                    CreateCell(item?.PaymentMethod),
                    CreateCell(item?.MyDepositAsset),
                    CreateCell(item?.DepositMonth.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.DepositDay.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.MaturityDate),
                    CreateCell(item?.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Note),
                    CreateCell(item?.Noticed.ToString()),
                    CreateCell(item?.Expired.ToString())
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

    #endregion

    private static DateTime ParseMaturityDate(string maturityDate)
    {
        var parts = Regex.Split(maturityDate, "-");
        return new DateTime(
            Convert.ToInt32(parts[0]),
            Convert.ToInt32(parts[1]),
            Convert.ToInt32(parts[2]));
    }
}
