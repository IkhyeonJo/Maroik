using System.Globalization;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers
{
    public class NoticeController(
        IHtmlLocalizer<NoticeController> localizer,
        IAssetRepository assetRepository,
        IFixedIncomeRepository fixedIncomeRepository,
        IFixedExpenditureRepository fixedExpenditureRepository)
        : Controller
    {
        #region FixedIncome

        #region Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateFixedIncome(
            [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        fixedIncomeInputViewModel.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(
                            EnumHelper.GetDescription(Session.Account), out var resultByte);

                        #region Validation of MainClass & SubClass Value

                        switch (fixedIncomeInputViewModel.MainClass)
                        {
                            case "RegularIncome" when fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"):
                            case "IrregularIncome" when fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"):
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region Validate of DepositMonth

                        if (fixedIncomeInputViewModel.DepositMonth is not (>= 1 and <= 12))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region Validate of DepositDay

                        switch (fixedIncomeInputViewModel.DepositMonth)
                        {
                            case 1 or
                                3 or
                                5 or
                                7 or
                                8 or
                                10 or
                                12 when fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 31):
                            case 2 when fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 29):
                            case 4 or
                                6 or
                                9 or
                                11 when fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 30):
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region MaturityDate 형식 체크 (윤년 또는 월에 따른 일도 체크함)

                        if (!new Regex(@"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$").IsMatch(
                                fixedIncomeInputViewModel
                                    .MaturityDate)) // https://stackoverflow.com/questions/5247219/regular-expression-to-detect-yyyy-mm-dd/15233484
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region 고정 수입 생성

                        await fixedIncomeRepository.CreateFixedIncomeAsync
                        (
                            new FixedIncome
                            {
                                AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                                MainClass = fixedIncomeInputViewModel.MainClass,
                                SubClass = fixedIncomeInputViewModel.SubClass,
                                Content = fixedIncomeInputViewModel.Content ?? "",
                                Amount = Math.Abs(fixedIncomeInputViewModel.Amount),
                                DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
                                DepositMonth = fixedIncomeInputViewModel.DepositMonth,
                                DepositDay = fixedIncomeInputViewModel.DepositDay,
                                MaturityDate = new DateTime(
                                    Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[0]),
                                    Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[1]),
                                    Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[2])),
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Note = fixedIncomeInputViewModel.Note ?? ""
                            }
                        );

                        #endregion

                        return Json(new
                        {
                            result = true, message = localizer["The fixedIncome has been successfully created."].Value
                        });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }

                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }

        #endregion

        #region Read

        [HttpGet]
        public async Task<IActionResult> FixedIncome(string wholeSearch)
        {
            if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
            List<FixedIncomeOutputViewModel> fixedIncomeOutputViewModels = [];

            #region FixedIncomes

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

            DateTime currentDate =
                new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            var assets = await assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            foreach (var item in (await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert
                         .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Where(item => string.IsNullOrEmpty(wholeSearch) ||
                         localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) ||
                         localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) ||
                         (item.Content ?? "").Contains(wholeSearch) ||
                         (item.Amount.TrimTrailingZeros()).Contains(wholeSearch) ||
                         (assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
                             ?.MonetaryUnit.ToString() ?? "").Contains(wholeSearch) ||
                         (item.DepositMonth.ToString()).Contains(wholeSearch) ||
                         (item.DepositDay.ToString()).Contains(wholeSearch) ||
                         (item.MaturityDate.ToString("yyyy-MM-dd")).Contains(wholeSearch) ||
                         (item.Note ?? "").Contains(wholeSearch) ||
                         (item.DepositMyAssetProductName ?? "").Contains(wholeSearch) ||
                         (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture))
                         .Contains(wholeSearch) ||
                         (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture))
                         .Contains(wholeSearch)))
            {
                bool noticedResult;

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
                        ServerSetting.NoticeMaturityDateDay;
                }
                catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                {
                    noticedResult = false;
                }

                if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                {
                    noticedResult = true;
                }

                fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                {
                    Id = item.Id,
                    MainClass = localizer[item.MainClass].Value,
                    SubClass = localizer[item.SubClass].Value,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets
                        .FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)
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

            #endregion

            var result = fixedIncomeOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedIncomeGrid", result);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsFixedIncomeExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var tempFixedIncomes =
                    await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (tempFixedIncomes == null)
                {
                    return Json(new { result = false, error = localizer["No fixedIncome exists"].Value });
                }

                var tempFixedIncome = tempFixedIncomes.FirstOrDefault(a => a.Id == id);

                if (tempFixedIncome == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

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
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> GetFixedIncomeAmountLabel(string productName)
        {
            try
            {
                var label = localizer["Amount"].Value;

                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var asset =
                    (await assetRepository.GetAssetsAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new Asset();

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
        public async Task<IActionResult> UpdateFixedIncome(
            [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        fixedIncomeInputViewModel.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(
                            EnumHelper.GetDescription(Session.Account), out var resultByte);

                        #region Validation of MainClass & SubClass Value

                        switch (fixedIncomeInputViewModel.MainClass)
                        {
                            case "RegularIncome" when fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"):
                            case "IrregularIncome" when fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"):
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region Validate of DepositMonth

                        if (fixedIncomeInputViewModel.DepositMonth is not (>= 1 and <= 12))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region Validate of DepositDay

                        if (fixedIncomeInputViewModel.DepositMonth is 1 or
                            3 or
                            5 or
                            7 or
                            8 or
                            10 or
                            12)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 31))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth == 2)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 29))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth is 4 or
                                 6 or
                                 9 or
                                 11)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 30))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }

                        #endregion

                        #region MaturityDate 형식 체크 (윤년 또는 월에 따른 일도 체크함)

                        if (!new Regex(@"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$").IsMatch(
                                fixedIncomeInputViewModel
                                    .MaturityDate)) // https://stackoverflow.com/questions/5247219/regular-expression-to-detect-yyyy-mm-dd/15233484
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #endregion

                        #region 고정 수입 업데이트

                        List<FixedIncome> tempFixedIncomes =
                            await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert
                                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                        if (tempFixedIncomes == null)
                        {
                            return Json(new { result = false, error = localizer["No fixedIncome exists"].Value });
                        }

                        var tempFixedIncome = tempFixedIncomes.FirstOrDefault(a => a.Id == fixedIncomeInputViewModel.Id);

                        if (tempFixedIncome == null)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        #region 고정 수입 업데이트

                        tempFixedIncome.MainClass = fixedIncomeInputViewModel.MainClass;
                        tempFixedIncome.SubClass = fixedIncomeInputViewModel.SubClass;
                        tempFixedIncome.Content = fixedIncomeInputViewModel.Content ?? "";
                        tempFixedIncome.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                        tempFixedIncome.DepositMyAssetProductName =
                            fixedIncomeInputViewModel.DepositMyAssetProductName;
                        tempFixedIncome.DepositMonth = fixedIncomeInputViewModel.DepositMonth;
                        tempFixedIncome.DepositDay = fixedIncomeInputViewModel.DepositDay;
                        tempFixedIncome.MaturityDate = new DateTime(
                            Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[0]),
                            Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[1]),
                            Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[2]));
                        tempFixedIncome.Updated = DateTime.UtcNow;
                        tempFixedIncome.Note = fixedIncomeInputViewModel.Note ?? "";
                        tempFixedIncome.Unpunctuality = fixedIncomeInputViewModel.Unpunctuality;

                        await fixedIncomeRepository.UpdateFixedIncomeAsync(tempFixedIncome);

                        #endregion

                        return Json(new
                        {
                            result = true,
                            message = localizer["The fixedIncome has been successfully updated."].Value
                        });

                        #endregion
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
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
        public async Task<IActionResult> DeleteFixedIncome(
            [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var tempFixedIncomes =
                    await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (tempFixedIncomes == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                var tempFixedIncome = tempFixedIncomes
                    .FirstOrDefault(a => a.Id == fixedIncomeInputViewModel.Id);

                if (tempFixedIncome == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                #region 고정 수입 삭제

                await fixedIncomeRepository.DeleteFixedIncomeAsync(tempFixedIncome);

                #endregion

                return Json(new
                {
                    result = true, message = localizer["The fixedIncome has been successfully deleted."].Value
                });
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
        public async Task<IActionResult> ExportExcelFixedIncome(string fileName = "")
        {
            await Task.Yield();

            var account = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(HttpContext.Session
                .TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte)
                ? resultByte
                : []));
            var loginedAccountTimeZoneIanaId = account.TimeZoneIanaId;
            var currentDate = DateTime.UtcNow.ToLocalTime();

            var assets = await assetRepository.GetAssetsAsync(account.Email);
            var fixedIncomeList = await fixedIncomeRepository.GetFixedIncomesAsync(account.Email);

            var fixedIncomeOutputViewModels = fixedIncomeList.Select(item =>
                {
                    bool noticedResult;

                    try
                    {
                        var maturityDate = new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay);
                        noticedResult = currentDate.Subtract(maturityDate).TotalDays <= 0 &&
                                        Math.Abs(currentDate.Subtract(maturityDate).TotalDays) <=
                                        ServerSetting.NoticeMaturityDateDay;
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

            MemoryStream stream = new();

            using (var spreadsheetDocument =
                   SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var sheets = spreadsheetDocument.WorkbookPart?.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart?.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet1"
                };
                sheets?.Append(sheet);

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
                sheetData?.Append(headerRow);

                foreach (var item in fixedIncomeOutputViewModels)
                {
                    var dataRow = new Row();
                    dataRow.Append(
                        CreateCell(item.MainClass),
                        CreateCell(item.SubClass),
                        CreateCell(item.Content),
                        CreateCell(item.Amount.ToString(CultureInfo.InvariantCulture)),
                        CreateCell(item.MonetaryUnit),
                        CreateCell(item.DepositMonth.ToString()),
                        CreateCell(item.DepositDay.ToString()),
                        CreateCell(item.MaturityDate),
                        CreateCell(item.Note),
                        CreateCell(item.DepositMyAssetProductName),
                        CreateCell(item.Created.ToString(CultureInfo.InvariantCulture)),
                        CreateCell(item.Updated.ToString(CultureInfo.InvariantCulture)),
                        CreateCell(item.Noticed.ToString()),
                        CreateCell(item.Expired.ToString())
                    );
                    sheetData?.Append(dataRow);
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

        #region FixedExpenditure

        #region Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateFixedExpenditure(
            [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    fixedExpenditureInputViewModel.Amount = Math.Abs(fixedExpenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(
                        EnumHelper.GetDescription(Session.Account), out var resultByte);

                    #region 대분류 = '정기저축'

                    switch (fixedExpenditureInputViewModel.MainClass)
                    {
                        case "RegularSavings" when fixedExpenditureInputViewModel.SubClass is "Deposit" or "Investment":
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod ==
                                    fixedExpenditureInputViewModel.MyDepositAsset)
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
                                        .Email, fixedExpenditureInputViewModel.PaymentMethod);
                                var incomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                        .Email, fixedExpenditureInputViewModel.MyDepositAsset);

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

                                #region 고정 지출 생성

                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                (
                                    new FixedExpenditure
                                    {
                                        AccountEmail = JsonConvert
                                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Content = fixedExpenditureInputViewModel.Content ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset,
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[0]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[1]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[2])),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                );

                                #endregion

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully created."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "RegularSavings":
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        case "NonConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment":
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod ==
                                    fixedExpenditureInputViewModel.MyDepositAsset)
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
                                        .Email, fixedExpenditureInputViewModel.PaymentMethod);
                                var incomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                        .Email, fixedExpenditureInputViewModel.MyDepositAsset);

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

                                #region 고정 지출 생성

                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert
                                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Content = fixedExpenditureInputViewModel.Content ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset,
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[0]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[1]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[2])),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                );

                                #endregion

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully created."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "NonConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer":
                            try
                            {
                                #region 고정 지출 생성

                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert
                                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Content = fixedExpenditureInputViewModel.Content ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[0]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[1]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[2])),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                );

                                #endregion

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully created."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "NonConsumerSpending":
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        case "ConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
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
                                #region 고정 지출 생성

                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                (
                                    new FixedExpenditure
                                    {
                                        AccountEmail = JsonConvert
                                            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Content = fixedExpenditureInputViewModel.Content ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[0]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[1]),
                                            Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate,
                                                "-")[2])),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                );

                                #endregion

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully created."].Value
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

                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }

        #endregion

        #region Read

        [HttpGet]
        public async Task<IActionResult> FixedExpenditure(string wholeSearch)
        {
            if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
            List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels = [];

            #region FixedExpenditures

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

            DateTime currentDate =
                new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            var assets = await assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            foreach (var item in await fixedExpenditureRepository.GetFixedExpendituresAsync(
                         JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email))
            {
                if (!string.IsNullOrEmpty(wholeSearch) &&
                    !localizer[item.MainClass ?? ""].Value.Contains(wholeSearch) &&
                    !localizer[item.SubClass ?? ""].Value.Contains(wholeSearch) &&
                    !(item.Content ?? "").Contains(wholeSearch) &&
                    !(item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) &&
                    !(assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod).MonetaryUnit
                        .ToString() ?? "").Contains(wholeSearch) &&
                    !(item.PaymentMethod ?? "").Contains(wholeSearch) &&
                    !(item.MyDepositAsset ?? "").Contains(wholeSearch) &&
                    !(item.DepositMonth.ToString() ?? "").Contains(wholeSearch) &&
                    !(item.DepositDay.ToString() ?? "").Contains(wholeSearch) &&
                    !(item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) &&
                    !(item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch) &&
                    !(item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture) ?? "")
                        .Contains(wholeSearch) &&
                    !(item.Note ?? "").Contains(wholeSearch)) continue;
                {
                    bool noticedResult;

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
                            ServerSetting.NoticeMaturityDateDay;
                    }
                    catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                    {
                        noticedResult = false;
                    }

                    if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                    {
                        noticedResult = true;
                    }

                    fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = localizer[item.MainClass].Value,
                        SubClass = localizer[item.SubClass].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)
                            .MonetaryUnit,
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

            #endregion

            var result = fixedExpenditureOutputViewModels
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated).AsQueryable();
            return PartialView("_FixedExpenditureGrid", result);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsFixedExpenditureExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var tempFixedExpenditures =
                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (tempFixedExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["No fixedExpenditure exists"].Value });
                }

                var tempFixedExpenditure =
                    tempFixedExpenditures.FirstOrDefault(a => a.Id == id);

                if (tempFixedExpenditure == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

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
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> GetFixedExpenditureAmountLabel(string productName)
        {
            try
            {
                var label = localizer["Amount"].Value;

                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var asset =
                    (await assetRepository.GetAssetsAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email)).Where(x => !x.Deleted).FirstOrDefault(x => x.ProductName == productName) ?? new Asset();

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
        public async Task<IActionResult> UpdateFixedExpenditure(
            [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    fixedExpenditureInputViewModel.Amount = Math.Abs(fixedExpenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(
                        EnumHelper.GetDescription(Session.Account), out var resultByte);

                    #region 대분류 = '정기저축'

                    switch (fixedExpenditureInputViewModel.MainClass)
                    {
                        case "RegularSavings" when fixedExpenditureInputViewModel.SubClass is "Deposit" or "Investment":
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod ==
                                    fixedExpenditureInputViewModel.MyDepositAsset)
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
                                        .Email, fixedExpenditureInputViewModel.PaymentMethod);
                                var incomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                        .Email, fixedExpenditureInputViewModel.MyDepositAsset);

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

                                var tempFixedExpenditures =
                                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);
                                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == fixedExpenditureInputViewModel.Id);

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Content = fixedExpenditureInputViewModel.Content ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2]));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset =
                                    fixedExpenditureInputViewModel.MyDepositAsset ?? "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully updated."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "RegularSavings":
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        case "NonConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment":
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod ==
                                    fixedExpenditureInputViewModel.MyDepositAsset)
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
                                        .Email, fixedExpenditureInputViewModel.PaymentMethod);
                                var incomeAsset = await assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? []))
                                        .Email, fixedExpenditureInputViewModel.MyDepositAsset);

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

                                var tempFixedExpenditures =
                                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);
                                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == fixedExpenditureInputViewModel.Id);

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Content = fixedExpenditureInputViewModel.Content ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2]));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset =
                                    fixedExpenditureInputViewModel.MyDepositAsset ?? "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully updated."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "NonConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer":
                            try
                            {
                                var tempFixedExpenditures =
                                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);
                                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == fixedExpenditureInputViewModel.Id);

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Content = fixedExpenditureInputViewModel.Content ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2]));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully updated."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        case "NonConsumerSpending":
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        case "ConsumerSpending" when fixedExpenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
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
                                var tempFixedExpenditures =
                                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);
                                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == fixedExpenditureInputViewModel.Id);

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert
                                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Content = fixedExpenditureInputViewModel.Content ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1]),
                                    Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2]));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new
                                {
                                    result = true,
                                    message = localizer["The fixedExpenditure has been successfully updated."].Value
                                });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                        default:
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                    #endregion
                }

                return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
        public async Task<IActionResult> DeleteFixedExpenditure(
            [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                var tempFixedExpenditures =
                    await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                        .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (tempFixedExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                var tempFixedExpenditure = tempFixedExpenditures.FirstOrDefault(a => a.Id == fixedExpenditureInputViewModel.Id);
                if (tempFixedExpenditure == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                await fixedExpenditureRepository.DeleteFixedExpenditureAsync(tempFixedExpenditure);
                return Json(new
                {
                    result = true,
                    message = localizer["The fixedExpenditure has been successfully deleted."].Value
                });
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
        public async Task<IActionResult> ExportExcelFixedExpenditure(string fileName = "")
        {
            await Task.Yield();

            var fixedExpenditureOutputViewModels =
                new List<FixedExpenditureOutputViewModel>();

            #region FixedExpenditures

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);

            var loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

            DateTime currentDate =
                new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]),
                    Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            var assets = await assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            foreach (var item in await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert
                         .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email))
            {
                bool noticedResult;

                try
                {
                    noticedResult =
                        currentDate.Subtract(new DateTime(
                            Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                            item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate
                            .Subtract(new DateTime(
                                Convert.ToInt32(
                                    Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]),
                                item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                }
                catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                {
                    noticedResult = false;
                }

                if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                {
                    noticedResult = true;
                }

                fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                {
                    MainClass = localizer[item.MainClass].Value,
                    SubClass = localizer[item.SubClass].Value,
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
                        CreateCell(item?.DepositMonth.ToString()),
                        CreateCell(item?.DepositDay.ToString()),
                        CreateCell(item?.MaturityDate),
                        CreateCell(item?.Created.ToString(CultureInfo.InvariantCulture)),
                        CreateCell(item?.Updated.ToString(CultureInfo.InvariantCulture)),
                        CreateCell(item?.Note),
                        CreateCell((item is { Noticed: true }).ToString()),
                        CreateCell((item is { Expired: true }).ToString())
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
}