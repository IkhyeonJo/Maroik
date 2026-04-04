using Mapster;
﻿using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class DashBoardController(
    IIncomeRepository incomeRepository,
    IExpenditureRepository expenditureRepository,
    IAssetRepository assetRepository,
    IAccountRepository accountRepository)
    : BaseController
{
    private const string HostResourceFilePath = "/app/Maroik.Log/HostResourceInfo.txt";
    private const string DockerResourceFilePath = "/app/Maroik.Log/DockerResourceInfo.txt";

    #region Language change
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public IActionResult CultureManagement([FromBody] LoginInputViewModel loginInputViewModel)
    {
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(loginInputViewModel.Culture)),
            new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict });

        return Json(new { result = true });
    }
    #endregion

    #region Admin
    [HttpGet]
    public IActionResult AdminIndex()
    {
        var hostResourceFileText = System.IO.File.Exists(HostResourceFilePath)
            ? System.IO.File.ReadAllText(HostResourceFilePath)
            : string.Empty;
        var dockerResourceFileText = System.IO.File.Exists(DockerResourceFilePath)
            ? System.IO.File.ReadAllText(DockerResourceFilePath)
            : string.Empty;

        static string ExtractSection(string input, string startMarker, string endMarker = null)
        {
            try
            {
                int startIndex = input.IndexOf(startMarker) + startMarker.Length;
                int endIndex = endMarker != null ? input.IndexOf(endMarker, startIndex) : input.Length;
                return input.Substring(startIndex, endIndex - startIndex).Trim();
            }
            catch { return ""; }
        }

        var hostCpuInfo = ExtractSection(hostResourceFileText, "Host CPU Information:", "Host Memory Information:");
        var hostMemoryInfo = ExtractSection(hostResourceFileText, "Host Memory Information:", "Host Disk Information:");
        var hostDiskInfo = ExtractSection(hostResourceFileText, "Host Disk Information:");
        var dockerContainerStatus = ExtractSection(dockerResourceFileText, "Docker Container Status:", "Docker Resource Usage:");
        var dockerResourceUsage = ExtractSection(dockerResourceFileText, "Docker Resource Usage:");

        var dockerContainerResult = new List<string> { "Name||CPU %||MEM Usage / LIMIT" };
        try
        {
            var resourceLines = dockerResourceUsage.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var resourceMap = new Dictionary<string, string[]>();
            for (int i = 1; i < resourceLines.Length; i++)
            {
                var columns = resourceLines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                resourceMap[columns[0]] = columns;
            }
            for (int i = 1; i < resourceLines.Length; i++)
            {
                var columns = resourceLines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string containerName = columns[0];
                if (resourceMap.TryGetValue(containerName, out var resourceDetails))
                {
                    string cpu = resourceDetails[1];
                    string memUsage = resourceDetails[2] + " " + resourceDetails[3] + " " + resourceDetails[4];
                    dockerContainerResult.Add($"{containerName}||{cpu}||{memUsage}");
                }
            }
        }
        catch { }

        string memUsageLimit = "";
        if (!string.IsNullOrEmpty(hostMemoryInfo))
        {
            try
            {
                string[] temp = hostMemoryInfo.Replace("Memory: ", "").Split('/');
                if (temp.Length >= 2)
                    memUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch { }
        }

        string diskUsageLimit = "";
        if (!string.IsNullOrEmpty(hostDiskInfo))
        {
            try
            {
                string[] temp = hostDiskInfo.Replace("Disk: ", "").Split('/');
                if (temp.Length >= 2)
                    diskUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch { }
        }

        double hostCpuNumeric = 0;
        if (!string.IsNullOrEmpty(hostCpuInfo))
            double.TryParse(new string(hostCpuInfo.Where(c => char.IsDigit(c) || c == '.').ToArray()), out hostCpuNumeric);

        ViewBag.HostCpuNumeric = hostCpuNumeric;
        ViewBag.HostCpuInfo = hostCpuInfo;
        ViewBag.MemUsageLimit = memUsageLimit;
        ViewBag.HostMemoryInfo = hostMemoryInfo;
        ViewBag.DiskUsageLimit = diskUsageLimit;
        ViewBag.HostDiskInfo = hostDiskInfo;
        ViewBag.DockerContainerResult = dockerContainerResult;

        return View();
    }
    #endregion

    #region User

    #region Read
    [HttpGet]
    public async Task<IActionResult> UserIndex(string year, string month)
    {
        _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        var loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        var userIndexOutputViewModel = new UserIndexOutputViewModel();
        var currentLoginUser = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email);
        var assets = (await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email)) ?? [];

        #region For Demo User [Only Demo purpose]
        if (currentLoginUser?.Email == "demo@maroik.com")
        {
            year = "2025";
            month = "06";
        }
        #endregion
        
        #region Year & Month
        if (string.IsNullOrEmpty(year))
        {
            year = DateTime.UtcNow.Year.ToString();
        }
        else
        {
            try
            {
                year = Convert.ToInt32(year).ToString();
            }
            catch
            {
                year = DateTime.UtcNow.Year.ToString();
            }
        }

        if (string.IsNullOrEmpty(month))
        {
            month = DateTime.UtcNow.Month.ToString("00");
        }
        else
        {
            try
            {
                month = Convert.ToInt32(month).ToString("00");
            }
            catch
            {
                month = DateTime.UtcNow.Month.ToString("00");
            }
        }
        #endregion

        #region Set if currentLoginUser's DefaultMonetaryUnit is not existed in Assets
        switch (currentLoginUser?.DefaultMonetaryUnit)
        {
            case null when assets.Count == 0:
            {
                if (currentLoginUser != null)
                {
                    currentLoginUser.DefaultMonetaryUnit = null;
                    await accountRepository.UpdateAccountAsync(currentLoginUser.Adapt<AccountRequest>());
                }

                break;
            }
            case null when assets.Count != 0:
            {
                var mostUsedMonetaryUnit = assets
                    .GroupBy(a => a.MonetaryUnit)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                
                if (currentLoginUser != null)
                {
                    currentLoginUser.DefaultMonetaryUnit = mostUsedMonetaryUnit?.Key;
                    await accountRepository.UpdateAccountAsync(currentLoginUser.Adapt<AccountRequest>());
                }

                break;
            }
            default:
            {
                if (currentLoginUser?.DefaultMonetaryUnit != null && assets.Count == 0)
                {
                    currentLoginUser.DefaultMonetaryUnit = null;
                    await accountRepository.UpdateAccountAsync(currentLoginUser.Adapt<AccountRequest>());
                }
                else if (currentLoginUser?.DefaultMonetaryUnit != null && assets.Count != 0)
                {
                    if (!assets.Select(x => x.MonetaryUnit).Distinct().ToList().Contains(currentLoginUser.DefaultMonetaryUnit))
                    {
                        var mostUsedMonetaryUnit = assets
                            .GroupBy(a => a.MonetaryUnit)
                            .OrderByDescending(g => g.Count())
                            .FirstOrDefault();
                        currentLoginUser.DefaultMonetaryUnit = mostUsedMonetaryUnit?.Key;
                        await accountRepository.UpdateAccountAsync(currentLoginUser.Adapt<AccountRequest>());
                    }
                }

                break;
            }
        }

        userIndexOutputViewModel.DefaultMonetaryUnit = currentLoginUser?.DefaultMonetaryUnit;
        #endregion
        
        #region Current Year Income/Expenditure & MonetaryUnits
        if (userIndexOutputViewModel.DefaultMonetaryUnit != null)
        {
            var currentYear = $"{year}";
            var currentLoginUserEmail = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email;

            var incomes = await incomeRepository.GetCurrentYearIncomesAsync(currentLoginUserEmail, currentYear, JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId);
            userIndexOutputViewModel.IncomeYearOutputViewModels = [];
            foreach (var item in incomes)
            {
                userIndexOutputViewModel.IncomeYearOutputViewModels.Add(new IncomeOutputViewModel()
                {
                    Id = item.Id,
                    MainClass = item.MainClass,
                    SubClass = item.SubClass,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)?.MonetaryUnit,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Note = item.Note
                });
            }

            var expenditures = await expenditureRepository.GetCurrentYearExpendituresAsync(currentLoginUserEmail, currentYear, JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId);
            userIndexOutputViewModel.ExpenditureYearOutputViewModels = [];
            foreach (var item in expenditures)
            {
                userIndexOutputViewModel.ExpenditureYearOutputViewModels.Add(new ExpenditureOutputViewModel()
                {
                    Id = item.Id,
                    MainClass = item.MainClass,
                    SubClass = item.SubClass,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                    PaymentMethod = item.PaymentMethod,
                    Note = item.Note,
                    MyDepositAsset = item.MyDepositAsset,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                });
            }
            userIndexOutputViewModel.IncomeYearOutputViewModels = userIndexOutputViewModel.IncomeYearOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
            userIndexOutputViewModel.ExpenditureYearOutputViewModels = userIndexOutputViewModel.ExpenditureYearOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
        }
        #endregion

        #region Current Year & Month Income/Expenditure & MonetaryUnits
        if (userIndexOutputViewModel.DefaultMonetaryUnit != null)
        {
            var currentYearMonth = $"{year}-{month}";
            var currentLoginUserEmail = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email;

            var incomes = await incomeRepository.GetCurrentYearMonthIncomesAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId);
            userIndexOutputViewModel.IncomeYearMonthOutputViewModels = [];
            foreach (var item in incomes)
            {
                userIndexOutputViewModel.IncomeYearMonthOutputViewModels.Add(new IncomeOutputViewModel()
                {
                    Id = item.Id,
                    MainClass = item.MainClass,
                    SubClass = item.SubClass,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)?.MonetaryUnit,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Note = item.Note
                });
            }

            var expenditures = await expenditureRepository.GetCurrentYearMonthExpendituresAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId);
            userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels = [];
            foreach (var item in expenditures)
            {
                userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels.Add(new ExpenditureOutputViewModel()
                {
                    Id = item.Id,
                    MainClass = item.MainClass,
                    SubClass = item.SubClass,
                    Content = item.Content,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                    PaymentMethod = item.PaymentMethod,
                    Note = item.Note,
                    MyDepositAsset = item.MyDepositAsset,
                    Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                    Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                });
            }
            userIndexOutputViewModel.IncomeYearMonthOutputViewModels = userIndexOutputViewModel.IncomeYearMonthOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
            userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels = userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
        }
        #endregion

        #region Set MonetaryUnits
        if (assets?.Count != 0)
        {
            userIndexOutputViewModel.MonetaryUnits = assets?.Select(x => x?.MonetaryUnit).Distinct().ToList() ?? [];
        }
        #endregion

        #region StartYear
        var firstCreatedIncome = (await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email)).OrderBy(x => x.Created).ToList().FirstOrDefault() ?? new IncomeResponse();
        var firstCreatedExpenditure = (await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email)).OrderBy(x => x.Created).ToList().FirstOrDefault() ?? new ExpenditureResponse();

        if (firstCreatedIncome.Created == DateTime.MinValue)
        {
            firstCreatedIncome.Created = DateTime.MaxValue;
        }

        if (firstCreatedExpenditure.Created == DateTime.MinValue)
        {
            firstCreatedExpenditure.Created = DateTime.MaxValue;
        }

        var firstCreatedIncomeOrExpenditure = DateTime.Compare(firstCreatedIncome.Created, firstCreatedExpenditure.Created) < 0
            ? firstCreatedIncome.Created
            : firstCreatedExpenditure.Created;
        if (firstCreatedIncomeOrExpenditure == DateTime.MaxValue)
        {
            firstCreatedIncomeOrExpenditure = DateTime.UtcNow;
        }

        firstCreatedIncomeOrExpenditure = firstCreatedIncomeOrExpenditure.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
        #endregion

        userIndexOutputViewModel.StartYear = firstCreatedIncomeOrExpenditure.Year;
        userIndexOutputViewModel.EndYear = DateTime.UtcNow.Year;
        userIndexOutputViewModel.SelectedYear = Convert.ToInt32(year);
        userIndexOutputViewModel.SelectedMonth = Convert.ToInt32(month);

        #region Compute aggregated statistics

        if (userIndexOutputViewModel.DefaultMonetaryUnit != null)
        {
            // ---- Year Income ----
            var regularIncomeYear = userIndexOutputViewModel.IncomeYearOutputViewModels.Where(x => x.MainClass == "RegularIncome").ToList();
            var regularIncomeTotalYear = regularIncomeYear.Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeLaborIncomeYear = regularIncomeYear.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeBusinessIncomeYear = regularIncomeYear.Where(x => x.SubClass == "BusinessIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomePensionIncomeYear = regularIncomeYear.Where(x => x.SubClass == "PensionIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeFinancialIncomeYear = regularIncomeYear.Where(x => x.SubClass == "FinancialIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeRentalIncomeYear = regularIncomeYear.Where(x => x.SubClass == "RentalIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeOtherIncomeYear = regularIncomeYear.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (regularIncomeTotalYear != 0)
            {
                userIndexOutputViewModel.PercentageOfRegularIncomeLaborIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomeLaborIncomeYear / (double)regularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfRegularIncomeBusinessIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomeBusinessIncomeYear / (double)regularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfRegularIncomePensionIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomePensionIncomeYear / (double)regularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfRegularIncomeFinancialIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomeFinancialIncomeYear / (double)regularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfRegularIncomeRentalIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomeRentalIncomeYear / (double)regularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfRegularIncomeOtherIncomeYear = 100 * ((double)userIndexOutputViewModel.RegularIncomeOtherIncomeYear / (double)regularIncomeTotalYear);
            }

            var irregularIncomeYear = userIndexOutputViewModel.IncomeYearOutputViewModels.Where(x => x.MainClass == "IrregularIncome").ToList();
            var irregularIncomeTotalYear = irregularIncomeYear.Sum(v => v.Amount);
            userIndexOutputViewModel.IrregularIncomeLaborIncomeYear = irregularIncomeYear.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.IrregularIncomeOtherIncomeYear = irregularIncomeYear.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (irregularIncomeTotalYear != 0)
            {
                userIndexOutputViewModel.PercentageOfIrregularIncomeLaborIncomeYear = 100 * ((double)userIndexOutputViewModel.IrregularIncomeLaborIncomeYear / (double)irregularIncomeTotalYear);
                userIndexOutputViewModel.PercentageOfIrregularIncomeOtherIncomeYear = 100 * ((double)userIndexOutputViewModel.IrregularIncomeOtherIncomeYear / (double)irregularIncomeTotalYear);
            }

            // ---- Year Expenditure ----
            var regularSavingsYear = userIndexOutputViewModel.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "RegularSavings").ToList();
            var regularSavingsTotalYear = regularSavingsYear.Sum(v => v.Amount);
            userIndexOutputViewModel.RegularSavingsDepositYear = regularSavingsYear.Where(x => x.SubClass == "Deposit").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularSavingsInvestmentYear = regularSavingsYear.Where(x => x.SubClass == "Investment").Sum(v => v.Amount);
            if (regularSavingsTotalYear != 0)
            {
                userIndexOutputViewModel.PercentageOfRegularSavingsDepositYear = 100 * ((double)userIndexOutputViewModel.RegularSavingsDepositYear / (double)regularSavingsTotalYear);
                userIndexOutputViewModel.PercentageOfRegularSavingsInvestmentYear = 100 * ((double)userIndexOutputViewModel.RegularSavingsInvestmentYear / (double)regularSavingsTotalYear);
            }

            var nonconsumerSpendingYear = userIndexOutputViewModel.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "NonConsumerSpending").ToList();
            var nonConsumerSpendingTotalYear = nonconsumerSpendingYear.Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingPublicPensionYear = nonconsumerSpendingYear.Where(x => x.SubClass == "PublicPension").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingDebtRepaymentYear = nonconsumerSpendingYear.Where(x => x.SubClass == "DebtRepayment").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingTaxYear = nonconsumerSpendingYear.Where(x => x.SubClass == "Tax").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingSocialInsuranceYear = nonconsumerSpendingYear.Where(x => x.SubClass == "SocialInsurance").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingInterHouseholdTransferExpensesYear = nonconsumerSpendingYear.Where(x => x.SubClass == "InterHouseholdTransferExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingNonProfitOrganizationTransferYear = nonconsumerSpendingYear.Where(x => x.SubClass == "NonProfitOrganizationTransfer").Sum(v => v.Amount);
            if (nonConsumerSpendingTotalYear != 0)
            {
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingPublicPensionYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingPublicPensionYear / (double)nonConsumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingDebtRepaymentYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingDebtRepaymentYear / (double)nonConsumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingTaxYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingTaxYear / (double)nonConsumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingSocialInsuranceYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingSocialInsuranceYear / (double)nonConsumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingInterHouseholdTransferExpensesYear / (double)nonConsumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingNonProfitOrganizationTransferYear / (double)nonConsumerSpendingTotalYear);
            }

            var consumerSpendingYear = userIndexOutputViewModel.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "ConsumerSpending").ToList();
            var consumerSpendingTotalYear = consumerSpendingYear.Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingMealOrEatOutExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "MealOrEatOutExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingHousingOrSuppliesCostYear = consumerSpendingYear.Where(x => x.SubClass == "HousingOrSuppliesCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingEducationExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "EducationExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingMedicalExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "MedicalExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingTransportationCostYear = consumerSpendingYear.Where(x => x.SubClass == "TransportationCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingCommunicationCostYear = consumerSpendingYear.Where(x => x.SubClass == "CommunicationCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingLeisureOrCultureYear = consumerSpendingYear.Where(x => x.SubClass == "LeisureOrCulture").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingClothingOrShoesYear = consumerSpendingYear.Where(x => x.SubClass == "ClothingOrShoes").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingPinMoneyYear = consumerSpendingYear.Where(x => x.SubClass == "PinMoney").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingProtectionTypeInsuranceYear = consumerSpendingYear.Where(x => x.SubClass == "ProtectionTypeInsurance").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingOtherExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "OtherExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingUnknownExpenditureYear = consumerSpendingYear.Where(x => x.SubClass == "UnknownExpenditure").Sum(v => v.Amount);
            if (consumerSpendingTotalYear != 0)
            {
                userIndexOutputViewModel.PercentageOfConsumerSpendingMealOrEatOutExpensesYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingMealOrEatOutExpensesYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingHousingOrSuppliesCostYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingHousingOrSuppliesCostYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingEducationExpensesYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingEducationExpensesYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingMedicalExpensesYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingMedicalExpensesYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingTransportationCostYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingTransportationCostYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingCommunicationCostYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingCommunicationCostYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingLeisureOrCultureYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingLeisureOrCultureYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingClothingOrShoesYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingClothingOrShoesYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingPinMoneyYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingPinMoneyYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingProtectionTypeInsuranceYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingProtectionTypeInsuranceYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingOtherExpensesYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingOtherExpensesYear / (double)consumerSpendingTotalYear);
                userIndexOutputViewModel.PercentageOfConsumerSpendingUnknownExpenditureYear = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingUnknownExpenditureYear / (double)consumerSpendingTotalYear);
            }

            // ---- YearMonth Income ----
            var regularIncomeYearMonth = userIndexOutputViewModel.IncomeYearMonthOutputViewModels.Where(x => x.MainClass == "RegularIncome").ToList();
            var regularIncomeTotalYearMonth = regularIncomeYearMonth.Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeLaborIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeBusinessIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "BusinessIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomePensionIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "PensionIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeFinancialIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "FinancialIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeRentalIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "RentalIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularIncomeOtherIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (regularIncomeTotalYearMonth != 0)
            {
                userIndexOutputViewModel.PercentageOfRegularIncomeLaborIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomeLaborIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularIncomeBusinessIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomeBusinessIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularIncomePensionIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomePensionIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularIncomeFinancialIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomeFinancialIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularIncomeRentalIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomeRentalIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularIncomeOtherIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.RegularIncomeOtherIncomeYearMonth / (double)regularIncomeTotalYearMonth);
            }

            var irregularIncomeYearMonth = userIndexOutputViewModel.IncomeYearMonthOutputViewModels.Where(x => x.MainClass == "IrregularIncome").ToList();
            var irregularIncomeTotalYearMonth = irregularIncomeYearMonth.Sum(v => v.Amount);
            userIndexOutputViewModel.IrregularIncomeLaborIncomeYearMonth = irregularIncomeYearMonth.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            userIndexOutputViewModel.IrregularIncomeOtherIncomeYearMonth = irregularIncomeYearMonth.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (irregularIncomeTotalYearMonth != 0)
            {
                userIndexOutputViewModel.PercentageOfIrregularIncomeLaborIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.IrregularIncomeLaborIncomeYearMonth / (double)irregularIncomeTotalYearMonth);
                userIndexOutputViewModel.PercentageOfIrregularIncomeOtherIncomeYearMonth = 100 * ((double)userIndexOutputViewModel.IrregularIncomeOtherIncomeYearMonth / (double)irregularIncomeTotalYearMonth);
            }

            // ---- YearMonth Expenditure ----
            var regularSavingsYearMonth = userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "RegularSavings").ToList();
            var regularSavingsTotalYearMonth = regularSavingsYearMonth.Sum(v => v.Amount);
            userIndexOutputViewModel.RegularSavingsDepositYearMonth = regularSavingsYearMonth.Where(x => x.SubClass == "Deposit").Sum(v => v.Amount);
            userIndexOutputViewModel.RegularSavingsInvestmentYearMonth = regularSavingsYearMonth.Where(x => x.SubClass == "Investment").Sum(v => v.Amount);
            if (regularSavingsTotalYearMonth != 0)
            {
                userIndexOutputViewModel.PercentageOfRegularSavingsDepositYearMonth = 100 * ((double)userIndexOutputViewModel.RegularSavingsDepositYearMonth / (double)regularSavingsTotalYearMonth);
                userIndexOutputViewModel.PercentageOfRegularSavingsInvestmentYearMonth = 100 * ((double)userIndexOutputViewModel.RegularSavingsInvestmentYearMonth / (double)regularSavingsTotalYearMonth);
            }

            var nonConsumerSpendingYearMonth = userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "NonConsumerSpending").ToList();
            var nonConsumerSpendingTotalYearMonth = nonConsumerSpendingYearMonth.Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingPublicPensionYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "PublicPension").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingDebtRepaymentYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "DebtRepayment").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingTaxYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "Tax").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingSocialInsuranceYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "SocialInsurance").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "InterHouseholdTransferExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.NonConsumerSpendingNonProfitOrganizationTransferYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "NonProfitOrganizationTransfer").Sum(v => v.Amount);
            if (nonConsumerSpendingTotalYearMonth != 0)
            {
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingPublicPensionYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingPublicPensionYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingDebtRepaymentYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingDebtRepaymentYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingTaxYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingTaxYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingSocialInsuranceYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingSocialInsuranceYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth = 100 * ((double)userIndexOutputViewModel.NonConsumerSpendingNonProfitOrganizationTransferYearMonth / (double)nonConsumerSpendingTotalYearMonth);
            }

            var consumerSpendingYearMonth = userIndexOutputViewModel.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "ConsumerSpending").ToList();
            var consumerSpendingTotalYearMonth = consumerSpendingYearMonth.Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingMealOrEatOutExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "MealOrEatOutExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingHousingOrSuppliesCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "HousingOrSuppliesCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingEducationExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "EducationExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingMedicalExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "MedicalExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingTransportationCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "TransportationCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingCommunicationCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "CommunicationCost").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingLeisureOrCultureYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "LeisureOrCulture").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingClothingOrShoesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "ClothingOrShoes").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingPinMoneyYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "PinMoney").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingProtectionTypeInsuranceYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "ProtectionTypeInsurance").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingOtherExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "OtherExpenses").Sum(v => v.Amount);
            userIndexOutputViewModel.ConsumerSpendingUnknownExpenditureYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "UnknownExpenditure").Sum(v => v.Amount);
            if (consumerSpendingTotalYearMonth != 0)
            {
                userIndexOutputViewModel.PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingMealOrEatOutExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingHousingOrSuppliesCostYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingEducationExpensesYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingEducationExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingMedicalExpensesYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingMedicalExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingTransportationCostYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingTransportationCostYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingCommunicationCostYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingCommunicationCostYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingLeisureOrCultureYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingLeisureOrCultureYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingClothingOrShoesYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingClothingOrShoesYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingPinMoneyYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingPinMoneyYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingProtectionTypeInsuranceYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingOtherExpensesYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingOtherExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                userIndexOutputViewModel.PercentageOfConsumerSpendingUnknownExpenditureYearMonth = 100 * ((double)userIndexOutputViewModel.ConsumerSpendingUnknownExpenditureYearMonth / (double)consumerSpendingTotalYearMonth);
            }
        }

        #endregion

        return View(userIndexOutputViewModel);
    }
    #endregion

    #region Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UserUpdateDefaultMonetary([FromBody] UserIndexInputViewModel userIndexInputViewModel)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);
            var assets = (await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email)) ?? [];

            var currentLoginUser = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email);

            currentLoginUser.DefaultMonetaryUnit = !assets.Select(x => x.MonetaryUnit).Distinct().ToList().Contains(userIndexInputViewModel.DefaultMonetaryUnit)
                ? null
                : userIndexInputViewModel.DefaultMonetaryUnit;

            await accountRepository.UpdateAccountAsync(currentLoginUser.Adapt<AccountRequest>());

            return Json(new { result = true });

        }
        catch
        {
            return Json(new { result = false });
        }
    }
    #endregion

    #endregion

    #region Anonymous
    [HttpGet]
    public IActionResult AnonymousIndex()
    {
        var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (!isAccountSessionExist) return View();
        var loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));
        return loginedAccount.Role switch
        {
            Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
            Role.User => RedirectToAction("UserIndex", "DashBoard"),
            _ => null
        };
    }
    #endregion
}
