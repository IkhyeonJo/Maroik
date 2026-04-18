using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers;

/// <summary>Manages fixed-income and fixed-expenditure notice records (create, read, update, delete, export).</summary>
public class NoticeController : BaseController
{
    private readonly IHtmlLocalizer<NoticeController> _localizer;
    private readonly ILogger<NoticeController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IAssetService _assetService;
    private readonly IFixedIncomeService _fixedIncomeService;
    private readonly IFixedExpenditureService _fixedExpenditureService;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="NoticeController"/> with the supplied dependencies.</summary>
    public NoticeController(
        IHtmlLocalizer<NoticeController> localizer,
        ILogger<NoticeController> logger,
        IHostEnvironment hostEnvironment,
        IAssetService assetService,
        IFixedIncomeService fixedIncomeService,
        IFixedExpenditureService fixedExpenditureService,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> settings,
        ISessionService sessionService)
        : base(accountService, dashboardService, settings, sessionService)
    {
        _localizer = localizer;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _assetService = assetService;
        _fixedIncomeService = fixedIncomeService;
        _fixedExpenditureService = fixedExpenditureService;
        _settings = settings;
        _sessionService = sessionService;
    }

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
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string? email = _sessionService.GetAccount()!.Email;

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
            MaturityDate = ParseMaturityDate(fixedIncomeInputViewModel.MaturityDate!),
            Note = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await _fixedIncomeService.CreateAsync(email!, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Read

    /// <summary>Returns the fixed-income grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedIncome(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<FixedIncomeOutputViewModel> fixedIncomeOutputViewModels = [];

            #region FixedIncomes

            string? loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            DateTime currentDate =
                new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email!);

            foreach (FixedIncomeResponse item in await _fixedIncomeService.GetFixedIncomesAsync(_sessionService.GetAccount()!.Email!))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    bool noticedResult = false;

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

                    fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass!.ToString()!].Value,
                        SubClass = _localizer[item.SubClass!.ToString()!].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                            .FirstOrDefault()!.MonetaryUnit,
                        DepositMonth = item.DepositMonth,
                        DepositDay = item.DepositDay,
                        MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                        Note = item.Note,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                        Noticed = noticedResult,
                        Unpunctuality = item.Unpunctuality,
                        Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault()
                            ?.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!).ToString() ?? "")
                        .Contains(wholeSearch))
                    {
                        bool noticedResult = false;

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

                        fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass!.ToString()!].Value,
                            SubClass = _localizer[item.SubClass!.ToString()!].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                                .FirstOrDefault()!.MonetaryUnit,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Note = item.Note,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                }
            }

            #endregion

            IQueryable<FixedIncomeOutputViewModel> result = fixedIncomeOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedIncomeGrid", result);
        }

        {
            AccountResponse loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            List<AssetResponse> assets = (await _assetService.GetAssetsAsync(loginedAccount.Email!))
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
            List<FixedIncomeResponse> tempFixedIncomes =
                await _fixedIncomeService.GetFixedIncomesAsync(_sessionService.GetAccount()!.Email!);

            if (tempFixedIncomes == null)
            {
                return Json(new { result = false, error = _localizer["No fixedIncome exists"].Value });
            }
            else
            {
                FixedIncomeResponse? tempFixedIncome = tempFixedIncomes.Where(a => a.Id == id).FirstOrDefault();

                if (tempFixedIncome == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    FixedIncomeOutputViewModel tempFixedIncomeOutputModel = new()
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
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            string label = _localizer["Amount"].Value;

            AssetResponse asset =
                (await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email!))
                .Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
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
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string? email = _sessionService.GetAccount()!.Email;

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
            MaturityDate = ParseMaturityDate(fixedIncomeInputViewModel.MaturityDate!),
            Note = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await _fixedIncomeService.UpdateAsync(email!, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
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
        string? email = _sessionService.GetAccount()!.Email;

        var result = await _fixedIncomeService.DeleteAsync(email!, fixedIncomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
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
        string? loginedAccountTimeZoneIanaId = account.TimeZoneIanaId;
        DateTime currentDate = DateTime.UtcNow.ToLocalTime();

        List<AssetResponse> assets = await _assetService.GetAssetsAsync(account.Email!);
        var fixedIncomeList = await _fixedIncomeService.GetFixedIncomesAsync(account.Email!);

        var fixedIncomeOutputViewModels = fixedIncomeList.Select(item =>
            {
                bool noticedResult = false;

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
                    MainClass = _localizer[item.MainClass!.ToString()!].Value,
                    SubClass = _localizer[item.SubClass!.ToString()!].Value,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = asset?.MonetaryUnit,
                    DepositMonth = item.DepositMonth,
                    DepositDay = item.DepositDay,
                    MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                    Note = item.Note,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
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

        MemoryStream stream = new();

        using (SpreadsheetDocument spreadsheetDocument =
               SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = spreadsheetDocument.WorkbookPart!.Workbook!.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            Cell CreateCell(string text)
            {
                return new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(text)
                };
            }

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.DepositMonth)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.DepositDay)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.MaturityDate)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.DepositMyAssetProductName)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Noticed)].Value),
                CreateCell(_localizer[nameof(FixedIncomeOutputViewModel.Expired)].Value)
            );
            sheetData!.Append(headerRow);

            foreach (var item in fixedIncomeOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.MainClass ?? ""),
                    CreateCell(item.SubClass ?? ""),
                    CreateCell(item.Content ?? ""),
                    CreateCell(item.Amount.ToString()),
                    CreateCell(item.MonetaryUnit ?? ""),
                    CreateCell(item.DepositMonth.ToString()),
                    CreateCell(item.DepositDay.ToString()),
                    CreateCell(item.MaturityDate ?? ""),
                    CreateCell(item.Note ?? ""),
                    CreateCell(item.DepositMyAssetProductName ?? ""),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Noticed.ToString()),
                    CreateCell(item.Expired.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";
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
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string? email = _sessionService.GetAccount()!.Email;

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
            MaturityDate = ParseMaturityDate(fixedExpenditureInputViewModel.MaturityDate!),
            Note = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await _fixedExpenditureService.CreateAsync(email!, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Read

    /// <summary>Returns the fixed-expenditure grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedExpenditure(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels = [];

            #region FixedExpenditures

            string? loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            DateTime currentDate =
                new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email!);

            foreach (FixedExpenditureResponse item in await _fixedExpenditureService.GetFixedExpendituresAsync(
                         _sessionService.GetAccount()!.Email!))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    bool noticedResult = false;

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

                    fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass!.ToString()!].Value,
                        SubClass = _localizer[item.SubClass!.ToString()!].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()!
                            .MonetaryUnit,
                        PaymentMethod = item.PaymentMethod,
                        MyDepositAsset = item.MyDepositAsset,
                        DepositMonth = item.DepositMonth,
                        DepositDay = item.DepositDay,
                        MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                        Note = item.Note,
                        Noticed = noticedResult,
                        Unpunctuality = item.Unpunctuality,
                        Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()?.MonetaryUnit
                            ?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.PaymentMethod?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch))
                    {
                        bool noticedResult = false;

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

                        fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass!.ToString()!].Value,
                            SubClass = _localizer[item.SubClass!.ToString()!].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()!
                                .MonetaryUnit,
                            PaymentMethod = item.PaymentMethod,
                            MyDepositAsset = item.MyDepositAsset,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                            Note = item.Note,
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                }
            }

            #endregion

            IQueryable<FixedExpenditureOutputViewModel> result = fixedExpenditureOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedExpenditureGrid", result);
        }

        {
            AccountResponse loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            List<AssetResponse> assets = (await _assetService.GetAssetsAsync(loginedAccount.Email!))
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
            List<FixedExpenditureResponse> tempFixedExpenditures =
                await _fixedExpenditureService.GetFixedExpendituresAsync(_sessionService.GetAccount()!.Email!);

            if (tempFixedExpenditures == null)
            {
                return Json(new { result = false, error = _localizer["No fixedExpenditure exists"].Value });
            }
            else
            {
                FixedExpenditureResponse? tempFixedExpenditure =
                    tempFixedExpenditures.Where(a => a.Id == id).FirstOrDefault();

                if (tempFixedExpenditure == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    FixedExpenditureOutputViewModel tempFixedIncomeOutputModel = new()
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
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            string label = _localizer["Amount"].Value;

            AssetResponse asset =
                (await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email!))
                .Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
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
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string? email = _sessionService.GetAccount()!.Email;

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
            MaturityDate = ParseMaturityDate(fixedExpenditureInputViewModel.MaturityDate!),
            Note = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await _fixedExpenditureService.UpdateAsync(email!, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
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
        string? email = _sessionService.GetAccount()!.Email;

        var result = await _fixedExpenditureService.DeleteAsync(email!, fixedExpenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #region Excel

    /// <summary>Exports fixed-expenditure records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelFixedExpenditure(string fileName = "")
    {
        await Task.Yield();

        List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels =
            new List<FixedExpenditureOutputViewModel>();

        #region FixedExpenditures

        string? loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

        DateTime currentDate =
            new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

        List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email!);

        foreach (FixedExpenditureResponse item in await _fixedExpenditureService.GetFixedExpendituresAsync(_sessionService.GetAccount()!.Email!))
        {
            bool noticedResult = false;

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

            fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
            {
                MainClass = _localizer[item.MainClass!.ToString()!].Value,
                SubClass = _localizer[item.SubClass!.ToString()!].Value,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()!.MonetaryUnit,
                PaymentMethod = item.PaymentMethod,
                MyDepositAsset = item.MyDepositAsset,
                DepositMonth = item.DepositMonth,
                DepositDay = item.DepositDay,
                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!),
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

        MemoryStream stream = new();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Worksheet worksheet = new Worksheet();
            SheetData sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            Sheet sheet = new Sheet()
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

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.PaymentMethod)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.MyDepositAsset)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.DepositMonth)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.DepositDay)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.MaturityDate)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Noticed)].Value),
                CreateCell(_localizer[nameof(FixedExpenditureOutputViewModel.Expired)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in fixedExpenditureOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(_localizer[item?.MainClass?.ToString() ?? ""].Value),
                    CreateCell(_localizer[item?.SubClass?.ToString() ?? ""].Value),
                    CreateCell(item?.Content ?? ""),
                    CreateCell(Convert.ToDecimal(item!.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit ?? ""),
                    CreateCell(item.PaymentMethod ?? ""),
                    CreateCell(item.MyDepositAsset ?? ""),
                    CreateCell(item.DepositMonth.ToString()),
                    CreateCell(item.DepositDay.ToString()),
                    CreateCell(item.MaturityDate ?? ""),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Note ?? ""),
                    CreateCell(item.Noticed.ToString()),
                    CreateCell(item.Expired.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId!):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

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
