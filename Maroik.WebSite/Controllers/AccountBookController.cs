using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages account-book records: assets, income transactions, and expenditure transactions
/// (create, read, update, delete, export).
/// </summary>
public class AccountBookController : BaseController
{
    private readonly IHtmlLocalizer<AccountBookController> _localizer;
    private readonly ILogger<AccountBookController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IAssetService _assetService;
    private readonly IIncomeService _incomeService;
    private readonly IExpenditureService _expenditureService;
    private readonly ISessionService _sessionService;

    public AccountBookController(
        IHtmlLocalizer<AccountBookController> localizer,
        ILogger<AccountBookController> logger,
        IHostEnvironment hostEnvironment,
        IAssetService assetService,
        IIncomeService incomeService,
        IExpenditureService expenditureService,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
        : base(accountService, dashboardService, serverSettings, sessionService)
    {
        _localizer = localizer;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _assetService = assetService;
        _incomeService = incomeService;
        _expenditureService = expenditureService;
        _sessionService = sessionService;
    }

    #region Asset

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Creates a new asset record.</summary>
    public async Task<IActionResult> CreateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

        var request = new AssetRequest
        {
            ProductName = assetInputViewModel.ProductName,
            Item = assetInputViewModel.Item,
            Amount = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note = assetInputViewModel.Note ?? "",
            Deleted = false
        };

        var result = await _assetService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    [HttpGet]
    /// <summary>Returns the asset grid (AJAX) or the full asset page.</summary>
    public async Task<IActionResult> Asset(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<AssetOutputViewModel> assetOutputViewModels = [];

            #region Assets

            string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            foreach (AssetResponse item in await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    assetOutputViewModels.Add(new AssetOutputViewModel()
                    {
                        ProductName = item.ProductName,
                        Item = _localizer[item.Item.ToString()].Value,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = item.MonetaryUnit,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note,
                        Deleted = item.Deleted
                    });
                }
                else
                {
                    if ((item.ProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        _localizer[item.Item?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (item.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Deleted.ToString() ?? "").Contains(wholeSearch))
                    {
                        assetOutputViewModels.Add(new AssetOutputViewModel()
                        {
                            ProductName = item.ProductName,
                            Item = _localizer[item.Item.ToString()].Value,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = item.MonetaryUnit,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Note = item.Note,
                            Deleted = item.Deleted
                        });
                    }
                }
            }

            #endregion

            IQueryable<AssetOutputViewModel> result =
                assetOutputViewModels.OrderBy(m => m.ProductName).AsQueryable();
            return PartialView("_AssetGrid", result);
        }

        {
            AccountResponse loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            string tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            List<AssetResponse> assets = (await _assetService.GetAssetsAsync(loginedAccount.Email))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Checks whether an asset with the given product name exists.</summary>
    public async Task<IActionResult> IsAssetExists(string productName)
    {
        try
        {
            AssetResponse tempAsset = await _assetService.GetAssetAsync(
                _sessionService.GetAccount()!.Email, productName);

            return tempAsset == null
                ? Json(new
                {
                    result = false, error = _localizer["Fail to find the asset by given product name"].Value
                })
                : (IActionResult)Json(new { result = true, asset = tempAsset });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Updates an existing asset record.</summary>
    public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        _ = ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

        var request = new AssetRequest
        {
            ProductName = assetInputViewModel.ProductName,
            Item = assetInputViewModel.Item,
            Amount = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note = assetInputViewModel.Note,
            Deleted = assetInputViewModel.Deleted
        };

        var result = await _assetService.UpdateAsync(email, request, assetInputViewModel.OriginalProductName);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Soft-deletes an existing asset record.</summary>
    public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email;

        var result = await _assetService.DeleteAsync(email, assetInputViewModel.ProductName);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Exports asset records to an Excel file.</summary>
    public async Task<IActionResult> ExportExcelAsset(string fileName = "")
    {
        await Task.Yield();

        List<AssetOutputViewModel> assetOutputViewModels = new List<AssetOutputViewModel>();

        string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

        foreach (AssetResponse item in await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
        {
            assetOutputViewModels.Add(new AssetOutputViewModel()
            {
                ProductName = item.ProductName,
                Item = item.Item,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = item.MonetaryUnit,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note,
                Deleted = item.Deleted
            });
        }

        assetOutputViewModels = assetOutputViewModels.OrderBy(m => m.ProductName).ToList();

        MemoryStream stream = new MemoryStream();
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
                CreateCell(_localizer[nameof(AssetOutputViewModel.ProductName)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Item)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Deleted)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in assetOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.ProductName),
                    CreateCell(_localizer[item?.Item?.ToString() ?? ""].Value),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Note),
                    CreateCell(item.Deleted.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Income

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Creates a new income transaction.</summary>
    public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

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

        var result = await _incomeService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Read

    [HttpGet]
    /// <summary>Returns the income grid (AJAX) or the full income page.</summary>
    public async Task<IActionResult> Income(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<IncomeOutputViewModel> incomeOutputViewModels = [];

            #region Incomes

            string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

            foreach (IncomeResponse item in await _incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    incomeOutputViewModels.Add(new IncomeOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass.ToString()].Value,
                        SubClass = _localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                            .FirstOrDefault().MonetaryUnit,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault()
                            .MonetaryUnit.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch))
                    {
                        incomeOutputViewModels.Add(new IncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass.ToString()].Value,
                            SubClass = _localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                                .FirstOrDefault().MonetaryUnit,
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
            AccountResponse loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            string tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            List<AssetResponse> assets = (await _assetService.GetAssetsAsync(loginedAccount.Email))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Checks whether an income record with the given ID exists.</summary>
    public async Task<IActionResult> IsIncomeExists(int id)
    {
        try
        {
            string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;
            List<IncomeResponse> tempIncomes = await _incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email);

            if (tempIncomes == null)
            {
                return Json(new { result = false, error = _localizer["No income exists"].Value });
            }
            else
            {
                IncomeResponse tempIncome = tempIncomes.Where(a => a.Id == id).FirstOrDefault();

                if (tempIncome == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    tempIncome.Created =
                        tempIncome.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                    return Json(new { result = true, income = tempIncome });
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Returns a formatted amount label for the given income product.</summary>
    public async Task<IActionResult> GetIncomeAmountLabel(string productName)
    {
        try
        {
            string label = _localizer["Amount"].Value;

            AssetResponse asset =
                (await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Updates an existing income record.</summary>
    public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

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

        var result = await _incomeService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Deletes an existing income record.</summary>
    public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email;

        var result = await _incomeService.DeleteAsync(email, incomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Exports income records to an Excel file.</summary>
    public async Task<IActionResult> ExportExcelIncome(string fileName = "")
    {
        await Task.Yield();

        List<IncomeOutputViewModel> incomeOutputViewModels = new List<IncomeOutputViewModel>();

        #region Incomes

        string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

        List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

        foreach (IncomeResponse item in await _incomeService.GetIncomesAsync(_sessionService.GetAccount()!.Email))
        {
            incomeOutputViewModels.Add(new IncomeOutputViewModel()
            {
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault()
                    .MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note
            });
        }

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
                CreateCell(_localizer[nameof(IncomeOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.DepositMyAssetProductName)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Note)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in incomeOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(_localizer[item?.MainClass?.ToString() ?? ""].Value),
                    CreateCell(_localizer[item?.SubClass?.ToString() ?? ""].Value),
                    CreateCell(item.Content),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.DepositMyAssetProductName),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Note)
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Expenditure

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Creates a new expenditure transaction.</summary>
    public async Task<IActionResult> CreateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

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

        var result = await _expenditureService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #region Read

    [HttpGet]
    /// <summary>Returns the expenditure grid (AJAX) or the full expenditure page.</summary>
    public async Task<IActionResult> Expenditure(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<ExpenditureOutputViewModel> expenditureOutputViewModels = [];

            #region Expenditures

            string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

            List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

            foreach (ExpenditureResponse item in await _expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass.ToString()].Value,
                        SubClass = _localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()
                            .MonetaryUnit,
                        PaymentMethod = item.PaymentMethod,
                        Note = item.Note,
                        MyDepositAsset = item.MyDepositAsset,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit
                            .ToString() ?? "").Contains(wholeSearch) ||
                        (item.PaymentMethod?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch))
                    {
                        expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass.ToString()].Value,
                            SubClass = _localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()
                                .MonetaryUnit,
                            PaymentMethod = item.PaymentMethod,
                            Note = item.Note,
                            MyDepositAsset = item.MyDepositAsset,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                        });
                    }
                }
            }

            #endregion

            IQueryable<ExpenditureOutputViewModel> result = expenditureOutputViewModels
                .OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
            return PartialView("_ExpenditureGrid", result);
        }

        {
            AccountResponse loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
            string tz = loginedAccount.TimeZoneIanaId ?? "UTC";
            List<AssetResponse> assets = (await _assetService.GetAssetsAsync(loginedAccount.Email))
                .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
            DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
            ViewBag.Assets = assets;
            ViewBag.CurrentDate = ct.ToString("yyyy-MM-dd");
            ViewBag.CurrentHour = ct.Hour;
            ViewBag.CurrentMinute = ct.Minute;
            ViewBag.CurrentSecond = ct.Second;
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Checks whether an expenditure record with the given ID exists.</summary>
    public async Task<IActionResult> IsExpenditureExists(int id)
    {
        try
        {
            string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;
            List<ExpenditureResponse> tempExpenditures =
                await _expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email);

            if (tempExpenditures == null)
            {
                return Json(new { result = false, error = _localizer["No expenditure exists"].Value });
            }
            else
            {
                ExpenditureResponse tempExpenditure = tempExpenditures.Where(a => a.Id == id).FirstOrDefault();

                if (tempExpenditure == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    tempExpenditure.Created =
                        tempExpenditure.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                    return Json(new { result = true, expenditure = tempExpenditure });
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Returns a formatted amount label for the given expenditure product.</summary>
    public async Task<IActionResult> GetExpenditureAmountLabel(string productName)
    {
        try
        {
            string label = _localizer["Amount"].Value;

            AssetResponse asset =
                (await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email))
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Updates an existing expenditure record.</summary>
    public async Task<IActionResult> UpdateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email;

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

        var result = await _expenditureService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Deletes an existing expenditure record.</summary>
    public async Task<IActionResult> DeleteExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email;

        var result = await _expenditureService.DeleteAsync(email, expenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    /// <summary>Exports expenditure records to an Excel file.</summary>
    public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
    {
        await Task.Yield();

        List<ExpenditureOutputViewModel> expenditureOutputViewModels = new List<ExpenditureOutputViewModel>();

        #region Expenditures

        string loginedAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;

        List<AssetResponse> assets = await _assetService.GetAssetsAsync(_sessionService.GetAccount()!.Email);

        foreach (ExpenditureResponse item in await _expenditureService.GetExpendituresAsync(_sessionService.GetAccount()!.Email))
        {
            expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
            {
                MainClass = _localizer[item.MainClass.ToString()].Value,
                SubClass = _localizer[item.SubClass.ToString()].Value,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit,
                PaymentMethod = item.PaymentMethod,
                Note = item.Note,
                MyDepositAsset = item.MyDepositAsset,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
            });
        }

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
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.PaymentMethod)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MyDepositAsset)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Updated)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in expenditureOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(_localizer[item?.MainClass?.ToString() ?? ""].Value),
                    CreateCell(_localizer[item?.SubClass?.ToString() ?? ""].Value),
                    CreateCell(item.Content),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.PaymentMethod),
                    CreateCell(item.Note),
                    CreateCell(item.MyDepositAsset),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion
}
