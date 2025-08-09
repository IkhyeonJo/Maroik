using System.Globalization;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using Maroik.Common.DataAccess.Data;

namespace Maroik.WebSite.Controllers;

public class AccountBookController(
    IHtmlLocalizer<AccountBookController> localizer,
    IAssetRepository assetRepository,
    IIncomeRepository incomeRepository,
    IExpenditureRepository expenditureRepository,
    IFixedExpenditureRepository fixedExpenditureRepository,
    ApplicationDbContext context)
    : Controller
{
    #region Asset

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            if (await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    assetInputViewModel.ProductName) != null)
            {
                return Json(new { result = false, error = localizer["The asset already exists."].Value });
            }

            try
            {
                #region Validation of item Value

                if (assetInputViewModel.Item is not ("FreeDepositAndWithdrawal" or
                    "TrustAsset" or
                    "CashAsset" or
                    "SavingsAsset" or
                    "InvestmentAsset" or
                    "RealEstate" or
                    "Movables" or
                    "OtherPhysicalAsset" or
                    "InsuranceAsset"))
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #endregion

                await assetRepository.CreateAssetAsync
                (
                    new Asset
                    {
                        ProductName = assetInputViewModel.ProductName,
                        AccountEmail = JsonConvert
                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                        Item = assetInputViewModel.Item,
                        Amount = assetInputViewModel.Amount,
                        MonetaryUnit = assetInputViewModel.MonetaryUnit,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        Note = assetInputViewModel.Note ?? "",
                        Deleted = false
                    }
                );
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            return Json(new
                { result = true, message = localizer["The asset has been successfully created."].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> Asset(string wholeSearch)
    {
        if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
        List<AssetOutputViewModel> assetOutputViewModels = [];

        #region Assets

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);

        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        assetOutputViewModels.AddRange(
            from item in await assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)
            where string.IsNullOrEmpty(wholeSearch) || (item.ProductName ?? "").Contains(wholeSearch) ||
                  localizer[item.Item ?? ""].Value.Contains(wholeSearch) ||
                  item.Amount.TrimTrailingZeros().Contains(wholeSearch) ||
                  (item.MonetaryUnit ?? "").Contains(wholeSearch) ||
                  item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                      .ToString(CultureInfo.InvariantCulture).Contains(wholeSearch) ||
                  item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                      .ToString(CultureInfo.InvariantCulture).Contains(wholeSearch) ||
                  (item.Note ?? "").Contains(wholeSearch) || item.Deleted.ToString().Contains(wholeSearch)
            select new AssetOutputViewModel
            {
                ProductName = item.ProductName,
                Item = localizer[item.Item].Value,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = item.MonetaryUnit,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note,
                Deleted = item.Deleted
            });

        #endregion

        var result = assetOutputViewModels.OrderBy(m => m.ProductName).AsQueryable();
        return PartialView("_AssetGrid", result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsAssetExists(string productName)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var tempAsset = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                productName);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            _ = ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            try
            {
                #region Validation of item Value

                if (assetInputViewModel.Item is not ("FreeDepositAndWithdrawal" or
                    "TrustAsset" or
                    "CashAsset" or
                    "SavingsAsset" or
                    "InvestmentAsset" or
                    "RealEstate" or
                    "Movables" or
                    "OtherPhysicalAsset" or
                    "InsuranceAsset"))
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #endregion

                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var tempAsset = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    assetInputViewModel.OriginalProductName);

                if (tempAsset == null)
                {
                    return Json(new
                    {
                        result = false, error = localizer["Fail to find the asset by given product name"].Value
                    });
                }

                tempAsset.ProductName = assetInputViewModel.ProductName;
                tempAsset.Item = assetInputViewModel.Item;
                tempAsset.Amount = assetInputViewModel.Amount;
                tempAsset.MonetaryUnit = assetInputViewModel.MonetaryUnit;
                tempAsset.Note = assetInputViewModel.Note;
                tempAsset.Deleted = assetInputViewModel.Deleted;
                tempAsset.Updated = DateTime.UtcNow;

                if (await assetRepository.UpdateAssetWithProductNameAsync(tempAsset,
                        assetInputViewModel.OriginalProductName) <= 0)
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });

                _ = await fixedExpenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                    tempAsset.AccountEmail, assetInputViewModel.OriginalProductName,
                    assetInputViewModel.ProductName);
                _ = await expenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                    tempAsset.AccountEmail, assetInputViewModel.OriginalProductName,
                    assetInputViewModel.ProductName);

                await transaction.CommitAsync();
                return Json(new
                {
                    result = true,
                    message = localizer["The asset has been successfully updated."].Value
                });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var tempAsset = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                assetInputViewModel.ProductName);

            if (tempAsset == null)
            {
                return Json(new
                    { result = false, error = localizer["Fail to find the asset by given product name"].Value });
            }

            tempAsset.Deleted = true;
            await assetRepository.UpdateAssetAsync(tempAsset);
            return Json(new
                { result = true, message = localizer["The asset has been successfully deleted."].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelAsset(string fileName = "")
    {
        await Task.Yield();

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var assetOutputViewModels =
            (await assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Select(item =>
                new AssetOutputViewModel
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
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros())
                        .ToString(CultureInfo.InvariantCulture)),
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            try
            {
                incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                #region Validate ProductName's Deletion

                var tempAssetToValidate = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    incomeInputViewModel.DepositMyAssetProductName);
                if (tempAssetToValidate.Deleted)
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer[
                            "Actions cannot be executed with assets that have already been deleted."].Value
                    });
                }

                #endregion

                #region Validation of MainClass & SubClass Value

                switch (incomeInputViewModel.MainClass)
                {
                    case "RegularIncome" when incomeInputViewModel.SubClass is not ("LaborIncome" or
                        "BusinessIncome" or
                        "PensionIncome" or
                        "FinancialIncome" or
                        "RentalIncome" or
                        "OtherIncome"):
                    case "IrregularIncome" when incomeInputViewModel.SubClass is not ("LaborIncome" or
                        "OtherIncome"):
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #endregion

                #region 수입 생성

                await incomeRepository.CreateIncomeAsync
                (
                    new Income
                    {
                        AccountEmail = JsonConvert
                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                        MainClass = incomeInputViewModel.MainClass,
                        SubClass = incomeInputViewModel.SubClass,
                        Content = incomeInputViewModel.Content ?? "",
                        Amount = Math.Abs(incomeInputViewModel.Amount),
                        DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
                        Created = incomeInputViewModel.Created,
                        Updated = DateTime.UtcNow,
                        Note = incomeInputViewModel.Note ?? ""
                    }
                );

                #endregion

                #region 자산 업데이트

                var tempAsset = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    incomeInputViewModel.DepositMyAssetProductName);
                tempAsset.Amount += Math.Abs(incomeInputViewModel.Amount);
                tempAsset.Updated = DateTime.UtcNow;
                await assetRepository.UpdateAssetAsync(tempAsset);

                #endregion

                await transaction.CommitAsync();
                return Json(new
                    { result = true, message = localizer["The income has been successfully created."].Value });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> Income(string wholeSearch)
    {
        if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
        List<IncomeOutputViewModel> incomeOutputViewModels = [];

        #region Incomes

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);

        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var assets = await assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

        incomeOutputViewModels.AddRange(from item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)
            where string.IsNullOrEmpty(wholeSearch) || localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) || localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) || (item.Content ?? "").Contains(wholeSearch) || item.Amount.TrimTrailingZeros().Contains(wholeSearch) || (assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                ?.MonetaryUnit.ToString() ?? "").Contains(wholeSearch) || (item.DepositMyAssetProductName ?? "").Contains(wholeSearch) || item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                .ToString(CultureInfo.InvariantCulture)
                .Contains(wholeSearch) || item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                .ToString(CultureInfo.InvariantCulture)
                .Contains(wholeSearch) || (item.Note ?? "").Contains(wholeSearch)
            select new IncomeOutputViewModel
            {
                Id = item.Id,
                MainClass = localizer[item.MainClass].Value,
                SubClass = localizer[item.SubClass].Value,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                    ?.MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note
            });

        #endregion

        var result = incomeOutputViewModels.OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Updated).AsQueryable();
        return PartialView("_IncomeGrid", result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsIncomeExists(int id)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;
            var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (tempIncomes == null)
            {
                return Json(new { result = false, error = localizer["No income exists"].Value });
            }

            var tempIncome = tempIncomes.FirstOrDefault(a => a.Id == id);

            if (tempIncome == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            tempIncome.Created =
                tempIncome.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
            return Json(new { result = true, income = tempIncome });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetIncomeAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var asset =
                (await assetRepository.GetAssetsAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email))
                .Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new Asset();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
            try
            {
                incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                #region Validation of MainClass & SubClass Value

                switch (incomeInputViewModel.MainClass)
                {
                    case "RegularIncome" when incomeInputViewModel.SubClass is not ("LaborIncome" or
                        "BusinessIncome" or
                        "PensionIncome" or
                        "FinancialIncome" or
                        "RentalIncome" or
                        "OtherIncome"):
                    case "IrregularIncome" when incomeInputViewModel.SubClass is not ("LaborIncome" or
                        "OtherIncome"):
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #endregion

                #region 수입 & 자산 업데이트

                var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (tempIncomes == null)
                {
                    return Json(new { result = false, error = localizer["No income exists"].Value });
                }

                var tempIncome = tempIncomes
                    .FirstOrDefault(a => a.Id == incomeInputViewModel.Id);

                if (tempIncome == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #region Validate ProductName's Deletion

                var tempAssetToValidate = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                        .Email, tempIncome.DepositMyAssetProductName);
                if (tempAssetToValidate.Deleted)
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer[
                                "Actions cannot be executed with assets that have already been deleted."]
                            .Value
                    });
                }

                #endregion

                var previousTempIncomeDepositMyAssetProductName =
                    tempIncome.DepositMyAssetProductName;
                var previousTempIncomeAmount = tempIncome.Amount;

                #region 수입 업데이트

                tempIncome.MainClass = incomeInputViewModel.MainClass;
                tempIncome.SubClass = incomeInputViewModel.SubClass;
                tempIncome.Content = incomeInputViewModel.Content;
                tempIncome.Amount = incomeInputViewModel.Amount;
                tempIncome.DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName;
                tempIncome.Note = incomeInputViewModel.Note;
                tempIncome.Created = incomeInputViewModel.Created;
                tempIncome.Updated = DateTime.UtcNow;

                await incomeRepository.UpdateIncomeAsync(tempIncome);

                #endregion

                #region 자산 업데이트

                if (tempIncome.DepositMyAssetProductName ==
                    previousTempIncomeDepositMyAssetProductName) // 같은 경우
                {
                    #region 자산 업데이트

                    var tempAsset = await assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                            .Email, tempIncome.DepositMyAssetProductName);
                    tempAsset.Amount = tempAsset.Amount - Math.Abs(previousTempIncomeAmount) +
                                       Math.Abs(tempIncome.Amount);
                    tempAsset.Updated = DateTime.UtcNow;
                    await assetRepository.UpdateAssetAsync(tempAsset);

                    #endregion
                }
                else // 다른 경우
                {
                    #region 구 자산 업데이트

                    var tempAsset = await assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                            .Email, previousTempIncomeDepositMyAssetProductName);
                    tempAsset.Amount -= Math.Abs(previousTempIncomeAmount);
                    tempAsset.Updated = DateTime.UtcNow;
                    await assetRepository.UpdateAssetAsync(tempAsset);

                    #endregion

                    #region 신 자산 업데이트

                    tempAsset = await assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                            .Email, tempIncome.DepositMyAssetProductName);
                    tempAsset.Amount += Math.Abs(tempIncome.Amount);
                    tempAsset.Updated = DateTime.UtcNow;
                    await assetRepository.UpdateAssetAsync(tempAsset);

                    #endregion
                }

                #endregion

                await transaction.CommitAsync();
                return Json(new
                {
                    result = true,
                    message = localizer["The income has been successfully updated."].Value
                });

                #endregion
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (tempIncomes == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            var tempIncome = tempIncomes.FirstOrDefault(a => a.Id == incomeInputViewModel.Id);

            if (tempIncome == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            #region Validate ProductName's Deletion

            var tempAssetToValidate = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                tempIncome.DepositMyAssetProductName);
            if (tempAssetToValidate.Deleted)
            {
                return Json(new
                {
                    result = false,
                    error = localizer[
                        "Actions cannot be executed with assets that have already been deleted."].Value
                });
            }

            #endregion

            #region 자산 업데이트

            var tempAsset = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                tempIncome.DepositMyAssetProductName);
            tempAsset.Amount -= Math.Abs(tempIncome.Amount);
            tempAsset.Updated = DateTime.UtcNow;
            await assetRepository.UpdateAssetAsync(tempAsset);

            #endregion

            #region 수입 삭제

            await incomeRepository.DeleteIncomeAsync(tempIncome);

            #endregion

            await transaction.CommitAsync();
            return Json(new
                { result = true, message = localizer["The income has been successfully deleted."].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelIncome(string fileName = "")
    {
        await Task.Yield();

        #region Incomes

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var assets = await assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

        var incomeOutputViewModels =
            (await incomeRepository.GetIncomesAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Select(item =>
                new IncomeOutputViewModel
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

            var sheet = new Sheet
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
                    CreateCell(item?.Content ?? ""),
                    CreateCell(Convert.ToDecimal(item?.Amount.TrimTrailingZeros())
                        .ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.MonetaryUnit ?? ""),
                    CreateCell(item?.DepositMyAssetProductName ?? ""),
                    CreateCell(item?.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item?.Note ?? "")
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

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                
            expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            #region Validate ProductName's Deletion

            var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                expenditureInputViewModel.PaymentMethod);
            if (tempExpenditureAssetToValidate.Deleted)
            {
                return Json(new
                {
                    result = false,
                    error = localizer["Actions cannot be executed with assets that have already been deleted."]
                        .Value
                });
            }

            if (!string.IsNullOrEmpty(expenditureInputViewModel.MyDepositAsset))
            {
                var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    expenditureInputViewModel.MyDepositAsset);
                if (tempIncomeAssetToValidate.Deleted)
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer[
                            "Actions cannot be executed with assets that have already been deleted."].Value
                    });
                }
            }

            #endregion

            #region 대분류 = '정기저축'

            switch (expenditureInputViewModel.MainClass)
            {
                case "RegularSavings" when expenditureInputViewModel.SubClass is "Deposit" or "Investment":
                    try
                    {
                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                    "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                            });
                        }

                        var expenditureAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.PaymentMethod);
                        var incomeAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.MyDepositAsset);

                        if (expenditureAsset == null || incomeAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                        "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                    .Value
                            });
                        }

                        #region 지출 생성

                        await expenditureRepository.CreateExpenditureAsync
                        (
                            new Expenditure
                            {
                                AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                    .Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel.Content ?? "",
                                Amount = Math.Abs(expenditureInputViewModel.Amount),
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel.Note ?? ""
                            }
                        );

                        #endregion

                        #region 지출 자산 업데이트

                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(expenditureAsset);

                        #endregion

                        #region 수입 자산 업데이트

                        incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                        incomeAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(incomeAsset);

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = localizer["The expenditure has been successfully created."].Value
                        });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                case "RegularSavings":
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                case "NonConsumerSpending"
                    when expenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment":
                    try
                    {
                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                    "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                            });
                        }

                        var expenditureAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.PaymentMethod);
                        var incomeAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.MyDepositAsset);

                        if (expenditureAsset == null || incomeAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                        "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                    .Value
                            });
                        }

                        #region 지출 생성

                        await expenditureRepository.CreateExpenditureAsync
                        (
                            new Expenditure
                            {
                                AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                    .Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel.Content ?? "",
                                Amount = Math.Abs(expenditureInputViewModel.Amount),
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel.Note ?? ""
                            }
                        );

                        #endregion

                        #region 지출 자산 업데이트

                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(expenditureAsset);

                        #endregion

                        #region 수입 자산 업데이트

                        incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                        incomeAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(incomeAsset);

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = localizer["The expenditure has been successfully created."].Value
                        });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                case "NonConsumerSpending" when expenditureInputViewModel.SubClass is "Tax" or
                    "SocialInsurance" or
                    "InterHouseholdTranserExpenses" or
                    "NonProfitOrganizationTransfer":
                    try
                    {
                        var expenditureAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.PaymentMethod);

                        if (expenditureAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #region 지출 생성

                        await expenditureRepository.CreateExpenditureAsync
                        (
                            new Expenditure
                            {
                                AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                    .Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel.Content ?? "",
                                Amount = Math.Abs(expenditureInputViewModel.Amount),
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel.Note ?? ""
                            }
                        );

                        #endregion

                        #region 지출 자산 업데이트

                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(expenditureAsset);

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = localizer["The expenditure has been successfully created."].Value
                        });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                case "NonConsumerSpending":
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                case "ConsumerSpending" when expenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
                    "HousingOrSuppliesCost" or
                    "EducationExpenses" or
                    "MedicalExpenses" or
                    "TransportationCost" or
                    "CommunicationCost" or
                    "LeisureOrCulture" or
                    "ClothingOrShoes" or
                    "PinMoney" or
                    "ProtectionTypeInsurance" or
                    "OtherExpenses" or
                    "UnknownExpenditure":
                    try
                    {
                        var expenditureAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.PaymentMethod);

                        if (expenditureAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #region 지출 생성

                        await expenditureRepository.CreateExpenditureAsync
                        (
                            new Expenditure
                            {
                                AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                    .Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel.Content ?? "",
                                Amount = Math.Abs(expenditureInputViewModel.Amount),
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel.Note ?? ""
                            }
                        );

                        #endregion

                        #region 지출 자산 업데이트

                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(expenditureAsset);

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = localizer["The expenditure has been successfully created."].Value
                        });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                case "ConsumerSpending":
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                default:
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            #endregion
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> Expenditure(string wholeSearch)
    {
        if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
        List<ExpenditureOutputViewModel> expenditureOutputViewModels = [];

        #region Expenditures

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);

        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var assets = await assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

        expenditureOutputViewModels.AddRange(
            from item in await expenditureRepository.GetExpendituresAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)
            where string.IsNullOrEmpty(wholeSearch) ||
                  localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) ||
                  localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) ||
                  (item.Content ?? "").Contains(wholeSearch) ||
                  item.Amount.TrimTrailingZeros().Contains(wholeSearch) ||
                  (assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit.ToString() ?? "")
                  .Contains(wholeSearch) || (item.PaymentMethod ?? "").Contains(wholeSearch) ||
                  (item.Note ?? "").Contains(wholeSearch) || (item.MyDepositAsset ?? "").Contains(wholeSearch) ||
                  item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                      .ToString(CultureInfo.InvariantCulture).Contains(wholeSearch) || item.Updated
                      .ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                      .ToString(CultureInfo.InvariantCulture).Contains(wholeSearch)
            select new ExpenditureOutputViewModel
            {
                Id = item.Id,
                MainClass = localizer[item.MainClass].Value,
                SubClass = localizer[item.SubClass].Value,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets
                    .FirstOrDefault(x => x.ProductName == item.PaymentMethod)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsExpenditureExists(int id)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;
            var tempExpenditures =
                await expenditureRepository.GetExpendituresAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (tempExpenditures == null)
            {
                return Json(new { result = false, error = localizer["No expenditure exists"].Value });
            }

            var tempExpenditure = tempExpenditures.FirstOrDefault(a => a.Id == id);

            if (tempExpenditure == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            tempExpenditure.Created =
                tempExpenditure.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
            return Json(new { result = true, expenditure = tempExpenditure });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetExpenditureAmountLabel(string productName)
    {
        try
        {
            var label = localizer["Amount"].Value;

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var asset =
                (await assetRepository.GetAssetsAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email))
                .Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new Asset();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                
            expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            #region 대분류 = '정기저축'

            switch (expenditureInputViewModel.MainClass)
            {
                case "RegularSavings":
                {
                    if (expenditureInputViewModel.SubClass is not ("Deposit" or "Investment"))
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    try
                    {
                        if (expenditureInputViewModel.PaymentMethod ==
                            expenditureInputViewModel.MyDepositAsset)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                    "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                            });
                        }

                        var expenditureAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(
                                    Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.PaymentMethod);
                        var incomeAsset = await assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<Account>(
                                    Encoding.Default.GetString(resultByte ?? []))
                                .Email, expenditureInputViewModel.MyDepositAsset);

                        if (expenditureAsset == null || incomeAsset == null)
                        {
                            return Json(new
                                { result = false, error = localizer["Input is invalid"].Value });
                        }

                        if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                        "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                    .Value
                            });
                        }

                        var tempExpenditures =
                            await expenditureRepository.GetExpendituresAsync(JsonConvert
                                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email);
                        var previousTempExpenditure =
                            tempExpenditures.FirstOrDefault(a => a.Id == expenditureInputViewModel.Id);
                        Expenditure currentTempExpenditure = new()
                        {
                            Id = expenditureInputViewModel.Id,
                            AccountEmail = JsonConvert
                                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email,
                            MainClass = expenditureInputViewModel.MainClass,
                            SubClass = expenditureInputViewModel.SubClass,
                            Content = expenditureInputViewModel.Content ?? "",
                            Amount = expenditureInputViewModel.Amount,
                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
                            MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
                            Created = expenditureInputViewModel.Created,
                            Updated = DateTime.UtcNow,
                            Note = expenditureInputViewModel.Note ?? "",
                            Asset = previousTempExpenditure?.Asset
                        };

                        #region Validate ProductName's Deletion

                        var tempExpenditureAssetToValidate =
                            await assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? []))
                                    .Email, previousTempExpenditure?.PaymentMethod);
                        if (tempExpenditureAssetToValidate.Deleted)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                        "Actions cannot be executed with assets that have already been deleted."]
                                    .Value
                            });
                        }

                        if (!string.IsNullOrEmpty(previousTempExpenditure?.MyDepositAsset))
                        {
                            var tempIncomeAssetToValidate =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);
                            if (tempIncomeAssetToValidate.Deleted)
                            {
                                return Json(new
                                {
                                    result = false,
                                    error = localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }
                        }

                        #endregion

                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]

                        if (string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) ||
                            !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) ||
                            string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) ||
                            string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                        {
                            if (string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) ||
                                string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) ||
                                string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) ||
                                string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                return Json(new
                                    { result = false, error = localizer["Input is invalid"].Value });

                            #region 지출 업데이트

                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                            #endregion

                            if (previousTempExpenditure.PaymentMethod ==
                                currentTempExpenditure.PaymentMethod)
                            {
                                if (previousTempExpenditure.MyDepositAsset ==
                                    currentTempExpenditure.MyDepositAsset)
                                {
                                    #region 자산 업데이트

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) +
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                                else
                                {
                                    #region 자산 업데이트

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    var currentIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    currentIncomeAsset.Amount +=
                                        Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                            }
                            else if (previousTempExpenditure.PaymentMethod ==
                                     currentTempExpenditure.MyDepositAsset)
                            {
                                if (previousTempExpenditure.MyDepositAsset ==
                                    currentTempExpenditure.PaymentMethod)
                                {
                                    #region 자산 업데이트

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) +
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) -
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                                else
                                {
                                    #region 자산 업데이트

                                    #region 과거 자산

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    var currentExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            currentTempExpenditure.PaymentMethod);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) +
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    #region 현재 자산

                                    currentExpenditureAsset.Amount -=
                                        Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    #endregion

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                            }
                            else if (previousTempExpenditure.MyDepositAsset ==
                                     currentTempExpenditure.PaymentMethod)
                            {
                                if (previousTempExpenditure.PaymentMethod ==
                                    currentTempExpenditure.MyDepositAsset)
                                {
                                    #region 자산 업데이트

                                    #region 과거 자산

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) +
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) -
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                                else
                                {
                                    #region 자산 업데이트

                                    #region 과거 자산

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    var currentIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount +=
                                        Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) -
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    #region 현재 자산

                                    currentIncomeAsset.Amount +=
                                        Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                    #endregion

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                            }
                            else if (previousTempExpenditure.MyDepositAsset ==
                                     currentTempExpenditure.MyDepositAsset)
                            {
                                if (previousTempExpenditure.PaymentMethod ==
                                    currentTempExpenditure.PaymentMethod)
                                {
                                    #region 자산 업데이트

                                    #region 과거 자산

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) +
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                                else
                                {
                                    #region 자산 업데이트

                                    #region 과거 자산

                                    var pastExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    var pastIncomeAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                    var currentExpenditureAsset =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            currentTempExpenditure.PaymentMethod);

                                    pastExpenditureAsset.Amount +=
                                        Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) +
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                    #endregion

                                    #region 현재 자산

                                    currentExpenditureAsset.Amount -=
                                        Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    #endregion

                                    #endregion

                                    //#region 자산 업데이트

                                    //#region 과거 자산
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                    //#endregion

                                    //#region 현재 자산
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    //#endregion

                                    //#endregion
                                }
                            }
                            else
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);

                                var currentExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        currentTempExpenditure.PaymentMethod);
                                var currentIncomeAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                pastIncomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                currentExpenditureAsset.Amount -=
                                    Math.Abs(currentTempExpenditure.Amount);
                                currentExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                currentIncomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = localizer["The expenditure has been successfully updated."]
                                    .Value
                            });

                            #endregion

                            #region 잘못된 경우

                        }

                        #region 지출 업데이트

                        await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                        #endregion

                        if (previousTempExpenditure.PaymentMethod ==
                            currentTempExpenditure.PaymentMethod) // else 보고 참조함.
                        {
                            var pastExpenditureAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.PaymentMethod);
                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                          Math.Abs(previousTempExpenditure.Amount) -
                                                          Math.Abs(currentTempExpenditure.Amount);
                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                            var currentIncomeAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    currentTempExpenditure.MyDepositAsset);
                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                            currentIncomeAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                        }
                        else if (previousTempExpenditure.PaymentMethod ==
                                 currentTempExpenditure.MyDepositAsset) // else 보고 참조함.
                        {
                            var pastExpenditureAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.PaymentMethod);
                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                          Math.Abs(previousTempExpenditure.Amount) +
                                                          Math.Abs(currentTempExpenditure.Amount);
                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                            var currentExpenditureAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    currentTempExpenditure.PaymentMethod);
                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                        }
                        else
                        {
                            #region 자산 업데이트

                            var pastExpenditureAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.PaymentMethod);
                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                            var currentExpenditureAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    currentTempExpenditure.PaymentMethod);
                            var currentIncomeAsset =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    currentTempExpenditure.MyDepositAsset);

                            #region 과거 자산

                            pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                            #endregion

                            #region 현재 자산

                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                            currentIncomeAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                            #endregion

                            #endregion
                        }

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = localizer["The expenditure has been successfully updated."].Value
                        });

                        #endregion
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                case "NonConsumerSpending":
                {
                    switch (expenditureInputViewModel.SubClass)
                    {
                        case "PublicPension" or "DebtRepayment":
                            try
                            {
                                if (expenditureInputViewModel.PaymentMethod ==
                                    expenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new
                                    {
                                        result = false,
                                        error = localizer[
                                                "The PaymentMethod and MyDepositAsset value cannot be the same."]
                                            .Value
                                    });
                                }

                                var expenditureAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? []))
                                        .Email, expenditureInputViewModel.PaymentMethod);
                                var incomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? []))
                                        .Email, expenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new
                                        { result = false, error = localizer["Input is invalid"].Value });
                                }

                                if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                                {
                                    return Json(new
                                    {
                                        result = false,
                                        error = localizer[
                                                "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                            .Value
                                    });
                                }

                                var tempExpenditures =
                                    await expenditureRepository.GetExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email);
                                var previousTempExpenditure =
                                    tempExpenditures.FirstOrDefault(a => a.Id == expenditureInputViewModel.Id);
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? "",
                                    Asset = previousTempExpenditure?.Asset
                                };

                                #region Validate ProductName's Deletion

                                var tempExpenditureAssetToValidate =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? []))
                                            .Email, previousTempExpenditure?.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new
                                    {
                                        result = false,
                                        error = localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }

                                if (!string.IsNullOrEmpty(previousTempExpenditure?.MyDepositAsset))
                                {
                                    var tempIncomeAssetToValidate =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new
                                        {
                                            result = false,
                                            error = localizer[
                                                    "Actions cannot be executed with assets that have already been deleted."]
                                                .Value
                                        });
                                    }
                                }

                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]

                                if (!string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) &&
                                    string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트

                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        var currentIncomeAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                        #endregion

                                        //#region 자산 업데이트

                                        //#region 과거 자산
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        //#endregion

                                        //#region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        //#endregion

                                        //#endregion
                                    }
                                    else if (previousTempExpenditure.PaymentMethod ==
                                             currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);

                                        var currentExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        #endregion

                                        #region 현재 자산

                                        currentExpenditureAsset.Amount -=
                                            Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        #endregion

                                        #endregion

                                        //#region 자산 업데이트

                                        //#region 과거 자산
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        //#endregion

                                        //#region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        var currentExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.PaymentMethod);
                                        var currentIncomeAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #region 현재 자산

                                        currentExpenditureAsset.Amount -=
                                            Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                        #endregion

                                        #endregion
                                    }

                                    await transaction.CommitAsync();
                                    return Json(new
                                    {
                                        result = true,
                                        message = localizer["The expenditure has been successfully updated."]
                                            .Value
                                    });
                                }

                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]

                                if (string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) ||
                                    string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) ||
                                    string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) ||
                                    string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                    return Json(new
                                        { result = false, error = localizer["Input is invalid"].Value });
                                    
                                {
                                    #region 지출 업데이트

                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset ==
                                            currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) -
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) +
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            var currentIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) -
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            currentIncomeAsset.Amount +=
                                                Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.PaymentMethod ==
                                             currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset ==
                                            currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) +
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) -
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            var currentExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) +
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            #region 현재 자산

                                            currentExpenditureAsset.Amount -=
                                                Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset ==
                                             currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.PaymentMethod ==
                                            currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) +
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) -
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            var currentIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount +=
                                                Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) -
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            #region 현재 자산

                                            currentIncomeAsset.Amount +=
                                                Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset ==
                                             currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.PaymentMethod ==
                                            currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                          Math.Abs(previousTempExpenditure.Amount) -
                                                                          Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) +
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산

                                            var pastExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.PaymentMethod);
                                            var pastIncomeAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    previousTempExpenditure.MyDepositAsset);

                                            var currentExpenditureAsset =
                                                await assetRepository.GetAssetAsync(
                                                    JsonConvert.DeserializeObject<Account>(
                                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                                    currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount +=
                                                Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                     Math.Abs(previousTempExpenditure.Amount) +
                                                                     Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            #endregion

                                            #region 현재 자산

                                            currentExpenditureAsset.Amount -=
                                                Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                        var currentExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.PaymentMethod);
                                        var currentIncomeAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #region 현재 자산

                                        currentExpenditureAsset.Amount -=
                                            Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                        #endregion

                                        #endregion
                                    }

                                    await transaction.CommitAsync();
                                    return Json(new
                                    {
                                        result = true,
                                        message = localizer["The expenditure has been successfully updated."]
                                            .Value
                                    });
                                }

                                #endregion
                            }
                            catch
                            {
                                return Json(new
                                    { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer":
                            try
                            {
                                var tempExpenditures =
                                    await expenditureRepository.GetExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email);
                                var previousTempExpenditure =
                                    tempExpenditures.FirstOrDefault(a => a.Id == expenditureInputViewModel.Id);
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? "",
                                    Asset = previousTempExpenditure?.Asset
                                };

                                #region Validate ProductName's Deletion

                                var tempExpenditureAssetToValidate =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? []))
                                            .Email, previousTempExpenditure?.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new
                                    {
                                        result = false,
                                        error = localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }

                                if (!string.IsNullOrEmpty(previousTempExpenditure?.MyDepositAsset))
                                {
                                    var tempIncomeAssetToValidate =
                                        await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new
                                        {
                                            result = false,
                                            error = localizer[
                                                    "Actions cannot be executed with assets that have already been deleted."]
                                                .Value
                                        });
                                    }
                                }

                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]

                                if (!string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) &&
                                    string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                    string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트

                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        var currentExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #region 현재 자산

                                        currentExpenditureAsset.Amount -=
                                            Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                        #endregion

                                        #endregion
                                    }

                                    await transaction.CommitAsync();
                                    return Json(new
                                    {
                                        result = true,
                                        message = localizer["The expenditure has been successfully updated."]
                                            .Value
                                    });
                                }

                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]

                                if (string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) ||
                                    string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) ||
                                    string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) ||
                                    !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                    return Json(new
                                        { result = false, error = localizer["Input is invalid"].Value });
                                    
                                {
                                    #region 지출 업데이트

                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #endregion
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset ==
                                             currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산

                                        var pastExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<Account>(
                                                Encoding.Default.GetString(resultByte ?? [])).Email,
                                            previousTempExpenditure.MyDepositAsset);

                                        var currentExpenditureAsset =
                                            await assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<Account>(
                                                    Encoding.Default.GetString(resultByte ?? [])).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                        #endregion

                                        #region 현재 자산

                                        currentExpenditureAsset.Amount -=
                                            Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        #endregion

                                        #endregion
                                    }

                                    await transaction.CommitAsync();
                                    return Json(new
                                    {
                                        result = true,
                                        message = localizer["The expenditure has been successfully updated."]
                                            .Value
                                    });
                                }

                                #endregion
                            }
                            catch
                            {
                                return Json(new
                                    { result = false, error = localizer["Input is invalid"].Value });
                            }

                        default:
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                case "ConsumerSpending":
                {
                    if (expenditureInputViewModel.SubClass is not ("MealOrEatOutExpenses" or
                        "HousingOrSuppliesCost" or
                        "EducationExpenses" or
                        "MedicalExpenses" or
                        "TransportationCost" or
                        "CommunicationCost" or
                        "LeisureOrCulture" or
                        "ClothingOrShoes" or
                        "PinMoney" or
                        "ProtectionTypeInsurance" or
                        "OtherExpenses" or
                        "UnknownExpenditure"))
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    try
                    {
                        var tempExpenditures =
                            await expenditureRepository.GetExpendituresAsync(JsonConvert
                                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                .Email);
                        var previousTempExpenditure =
                            tempExpenditures.FirstOrDefault(a => a.Id == expenditureInputViewModel.Id);
                        Expenditure currentTempExpenditure = new()
                        {
                            Id = expenditureInputViewModel.Id,
                            AccountEmail = JsonConvert
                                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                            MainClass = expenditureInputViewModel.MainClass,
                            SubClass = expenditureInputViewModel.SubClass,
                            Content = expenditureInputViewModel.Content ?? "",
                            Amount = expenditureInputViewModel.Amount,
                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
                            MyDepositAsset = "",
                            Created = expenditureInputViewModel.Created,
                            Updated = DateTime.UtcNow,
                            Note = expenditureInputViewModel.Note ?? "",
                            Asset = previousTempExpenditure?.Asset
                        };

                        #region Validate ProductName's Deletion

                        var tempExpenditureAssetToValidate =
                            await assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? []))
                                    .Email, previousTempExpenditure?.PaymentMethod);
                        if (tempExpenditureAssetToValidate.Deleted)
                        {
                            return Json(new
                            {
                                result = false,
                                error = localizer[
                                        "Actions cannot be executed with assets that have already been deleted."]
                                    .Value
                            });
                        }

                        if (!string.IsNullOrEmpty(previousTempExpenditure?.MyDepositAsset))
                        {
                            var tempIncomeAssetToValidate =
                                await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);
                            if (tempIncomeAssetToValidate.Deleted)
                            {
                                return Json(new
                                {
                                    result = false,
                                    error = localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }
                        }

                        #endregion

                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]

                        if (!string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) &&
                            string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                            !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                            string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                        {
                            #region 지출 업데이트

                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                            #endregion

                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                              Math.Abs(previousTempExpenditure.Amount) -
                                                              Math.Abs(currentTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                //pastIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                //currentIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }
                            else
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                var currentExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        currentTempExpenditure.PaymentMethod);
                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                //pastIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                currentExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                //currentIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = localizer["The expenditure has been successfully updated."].Value
                            });
                        }

                        #endregion

                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]

                        if (string.IsNullOrEmpty(previousTempExpenditure?.PaymentMethod) ||
                            string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) ||
                            string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) ||
                            !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            
                        {
                            #region 지출 업데이트

                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);

                            #endregion

                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);

                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                              Math.Abs(previousTempExpenditure.Amount) -
                                                              Math.Abs(currentTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                pastIncomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                //currentIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }
                            else if (previousTempExpenditure.MyDepositAsset ==
                                     currentTempExpenditure.PaymentMethod)
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);

                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                         Math.Abs(previousTempExpenditure.Amount) -
                                                         Math.Abs(currentTempExpenditure.Amount);
                                pastIncomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                //currentIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }
                            else
                            {
                                #region 자산 업데이트

                                #region 과거 자산

                                var pastExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        previousTempExpenditure.PaymentMethod);
                                var pastIncomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(
                                        Encoding.Default.GetString(resultByte ?? [])).Email,
                                    previousTempExpenditure.MyDepositAsset);

                                var currentExpenditureAsset =
                                    await assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<Account>(
                                            Encoding.Default.GetString(resultByte ?? [])).Email,
                                        currentTempExpenditure.PaymentMethod);
                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                pastExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                pastIncomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                #endregion

                                #region 현재 자산

                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                currentExpenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                //currentIncomeAsset.Updated = DateTime.UtcNow;
                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);

                                #endregion

                                #endregion
                            }

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = localizer["The expenditure has been successfully updated."].Value
                            });
                        }

                        #endregion
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                default:
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            #endregion
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var tempExpenditures =
                await expenditureRepository.GetExpendituresAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (tempExpenditures == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            var tempExpenditure = tempExpenditures
                .FirstOrDefault(a => a.Id == expenditureInputViewModel.Id);
            if (tempExpenditure == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            #region Validate ProductName's Deletion

            var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                tempExpenditure.PaymentMethod);
            if (tempExpenditureAssetToValidate.Deleted)
            {
                return Json(new
                {
                    result = false,
                    error = localizer[
                        "Actions cannot be executed with assets that have already been deleted."].Value
                });
            }

            if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
            {
                var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    tempExpenditure.MyDepositAsset);
                if (tempIncomeAssetToValidate.Deleted)
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer[
                            "Actions cannot be executed with assets that have already been deleted."].Value
                    });
                }
            }

            #endregion

            if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
            {
                #region 지출 삭제

                await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);

                #endregion

                #region 자산 업데이트

                var expenditureAsset = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    tempExpenditure.PaymentMethod);
                var incomeAsset = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    tempExpenditure.MyDepositAsset);

                expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                expenditureAsset.Updated = DateTime.UtcNow;
                await assetRepository.UpdateAssetAsync(expenditureAsset);

                incomeAsset.Amount -= Math.Abs(tempExpenditure.Amount);
                incomeAsset.Updated = DateTime.UtcNow;
                await assetRepository.UpdateAssetAsync(incomeAsset);

                #endregion

                await transaction.CommitAsync();
                return Json(new
                {
                    result = true,
                    message = localizer["The expenditure has been successfully deleted."].Value
                });
            }
            else
            {
                #region 지출 삭제

                await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);

                #endregion

                #region 자산 업데이트

                var expenditureAsset = await assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                    tempExpenditure.PaymentMethod);

                expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                expenditureAsset.Updated = DateTime.UtcNow;
                await assetRepository.UpdateAssetAsync(expenditureAsset);

                #endregion

                await transaction.CommitAsync();
                return Json(new
                {
                    result = true,
                    message = localizer["The expenditure has been successfully deleted."].Value
                });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
    {
        await Task.Yield();

        #region Expenditures

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out byte[] resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var assets = await assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

        var expenditureOutputViewModels =
            (await expenditureRepository.GetExpendituresAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Select(item =>
                new ExpenditureOutputViewModel
                {
                    MainClass = localizer[item.MainClass.ToString()].Value,
                    SubClass = localizer[item.SubClass.ToString()].Value,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                    PaymentMethod = item.PaymentMethod,
                    Note = item.Note,
                    MyDepositAsset = item.MyDepositAsset,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
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
                    CreateCell(Convert.ToDecimal(item?.Amount.TrimTrailingZeros())
                        .ToString(CultureInfo.InvariantCulture)),
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