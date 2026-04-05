using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class DashBoardController(IDashboardService dashboardService) : BaseController
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
        var loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? []));
        string email = loginedAccount.Email;
        string timeZoneId = loginedAccount.TimeZoneIanaId;

        #region For Demo User [Only Demo purpose]
        if (email == "demo@maroik.com")
        {
            year = "2025";
            month = "06";
        }
        #endregion

        var summary = await dashboardService.GetSummaryAsync(email, year ?? "", month ?? "", timeZoneId);

        var vm = new UserIndexOutputViewModel
        {
            DefaultMonetaryUnit = summary.DefaultMonetaryUnit,
            MonetaryUnits = summary.MonetaryUnits,
            StartYear = summary.StartYear,
            EndYear = summary.EndYear,
            SelectedYear = summary.SelectedYear,
            SelectedMonth = summary.SelectedMonth
        };

        if (summary.DefaultMonetaryUnit != null)
        {
            vm.IncomeYearOutputViewModels = summary.YearIncomes.Select(item => new IncomeOutputViewModel
            {
                Id = item.Id,
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = summary.Assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)?.MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Note = item.Note
            }).ToList();

            vm.ExpenditureYearOutputViewModels = summary.YearExpenditures.Select(item => new ExpenditureOutputViewModel
            {
                Id = item.Id,
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = summary.Assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                PaymentMethod = item.PaymentMethod,
                Note = item.Note,
                MyDepositAsset = item.MyDepositAsset,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId)
            }).ToList();

            vm.IncomeYearMonthOutputViewModels = summary.YearMonthIncomes.Select(item => new IncomeOutputViewModel
            {
                Id = item.Id,
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = summary.Assets.FirstOrDefault(x => x.ProductName == item.DepositMyAssetProductName)?.MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Note = item.Note
            }).ToList();

            vm.ExpenditureYearMonthOutputViewModels = summary.YearMonthExpenditures.Select(item => new ExpenditureOutputViewModel
            {
                Id = item.Id,
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = summary.Assets.FirstOrDefault(x => x.ProductName == item.PaymentMethod)?.MonetaryUnit,
                PaymentMethod = item.PaymentMethod,
                Note = item.Note,
                MyDepositAsset = item.MyDepositAsset,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId)
            }).ToList();

            // ---- Year Income ----
            var regularIncomeYear = vm.IncomeYearOutputViewModels.Where(x => x.MainClass == "RegularIncome").ToList();
            var regularIncomeTotalYear = regularIncomeYear.Sum(v => v.Amount);
            vm.RegularIncomeLaborIncomeYear = regularIncomeYear.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            vm.RegularIncomeBusinessIncomeYear = regularIncomeYear.Where(x => x.SubClass == "BusinessIncome").Sum(v => v.Amount);
            vm.RegularIncomePensionIncomeYear = regularIncomeYear.Where(x => x.SubClass == "PensionIncome").Sum(v => v.Amount);
            vm.RegularIncomeFinancialIncomeYear = regularIncomeYear.Where(x => x.SubClass == "FinancialIncome").Sum(v => v.Amount);
            vm.RegularIncomeRentalIncomeYear = regularIncomeYear.Where(x => x.SubClass == "RentalIncome").Sum(v => v.Amount);
            vm.RegularIncomeOtherIncomeYear = regularIncomeYear.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (regularIncomeTotalYear != 0)
            {
                vm.PercentageOfRegularIncomeLaborIncomeYear = 100 * ((double)vm.RegularIncomeLaborIncomeYear / (double)regularIncomeTotalYear);
                vm.PercentageOfRegularIncomeBusinessIncomeYear = 100 * ((double)vm.RegularIncomeBusinessIncomeYear / (double)regularIncomeTotalYear);
                vm.PercentageOfRegularIncomePensionIncomeYear = 100 * ((double)vm.RegularIncomePensionIncomeYear / (double)regularIncomeTotalYear);
                vm.PercentageOfRegularIncomeFinancialIncomeYear = 100 * ((double)vm.RegularIncomeFinancialIncomeYear / (double)regularIncomeTotalYear);
                vm.PercentageOfRegularIncomeRentalIncomeYear = 100 * ((double)vm.RegularIncomeRentalIncomeYear / (double)regularIncomeTotalYear);
                vm.PercentageOfRegularIncomeOtherIncomeYear = 100 * ((double)vm.RegularIncomeOtherIncomeYear / (double)regularIncomeTotalYear);
            }

            var irregularIncomeYear = vm.IncomeYearOutputViewModels.Where(x => x.MainClass == "IrregularIncome").ToList();
            var irregularIncomeTotalYear = irregularIncomeYear.Sum(v => v.Amount);
            vm.IrregularIncomeLaborIncomeYear = irregularIncomeYear.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            vm.IrregularIncomeOtherIncomeYear = irregularIncomeYear.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (irregularIncomeTotalYear != 0)
            {
                vm.PercentageOfIrregularIncomeLaborIncomeYear = 100 * ((double)vm.IrregularIncomeLaborIncomeYear / (double)irregularIncomeTotalYear);
                vm.PercentageOfIrregularIncomeOtherIncomeYear = 100 * ((double)vm.IrregularIncomeOtherIncomeYear / (double)irregularIncomeTotalYear);
            }

            // ---- Year Expenditure ----
            var regularSavingsYear = vm.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "RegularSavings").ToList();
            var regularSavingsTotalYear = regularSavingsYear.Sum(v => v.Amount);
            vm.RegularSavingsDepositYear = regularSavingsYear.Where(x => x.SubClass == "Deposit").Sum(v => v.Amount);
            vm.RegularSavingsInvestmentYear = regularSavingsYear.Where(x => x.SubClass == "Investment").Sum(v => v.Amount);
            if (regularSavingsTotalYear != 0)
            {
                vm.PercentageOfRegularSavingsDepositYear = 100 * ((double)vm.RegularSavingsDepositYear / (double)regularSavingsTotalYear);
                vm.PercentageOfRegularSavingsInvestmentYear = 100 * ((double)vm.RegularSavingsInvestmentYear / (double)regularSavingsTotalYear);
            }

            var nonconsumerSpendingYear = vm.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "NonConsumerSpending").ToList();
            var nonConsumerSpendingTotalYear = nonconsumerSpendingYear.Sum(v => v.Amount);
            vm.NonConsumerSpendingPublicPensionYear = nonconsumerSpendingYear.Where(x => x.SubClass == "PublicPension").Sum(v => v.Amount);
            vm.NonConsumerSpendingDebtRepaymentYear = nonconsumerSpendingYear.Where(x => x.SubClass == "DebtRepayment").Sum(v => v.Amount);
            vm.NonConsumerSpendingTaxYear = nonconsumerSpendingYear.Where(x => x.SubClass == "Tax").Sum(v => v.Amount);
            vm.NonConsumerSpendingSocialInsuranceYear = nonconsumerSpendingYear.Where(x => x.SubClass == "SocialInsurance").Sum(v => v.Amount);
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYear = nonconsumerSpendingYear.Where(x => x.SubClass == "InterHouseholdTranserExpenses").Sum(v => v.Amount);
            vm.NonConsumerSpendingNonProfitOrganizationTransferYear = nonconsumerSpendingYear.Where(x => x.SubClass == "NonProfitOrganizationTransfer").Sum(v => v.Amount);
            if (nonConsumerSpendingTotalYear != 0)
            {
                vm.PercentageOfNonConsumerSpendingPublicPensionYear = 100 * ((double)vm.NonConsumerSpendingPublicPensionYear / (double)nonConsumerSpendingTotalYear);
                vm.PercentageOfNonConsumerSpendingDebtRepaymentYear = 100 * ((double)vm.NonConsumerSpendingDebtRepaymentYear / (double)nonConsumerSpendingTotalYear);
                vm.PercentageOfNonConsumerSpendingTaxYear = 100 * ((double)vm.NonConsumerSpendingTaxYear / (double)nonConsumerSpendingTotalYear);
                vm.PercentageOfNonConsumerSpendingSocialInsuranceYear = 100 * ((double)vm.NonConsumerSpendingSocialInsuranceYear / (double)nonConsumerSpendingTotalYear);
                vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear = 100 * ((double)vm.NonConsumerSpendingInterHouseholdTransferExpensesYear / (double)nonConsumerSpendingTotalYear);
                vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear = 100 * ((double)vm.NonConsumerSpendingNonProfitOrganizationTransferYear / (double)nonConsumerSpendingTotalYear);
            }

            var consumerSpendingYear = vm.ExpenditureYearOutputViewModels.Where(x => x.MainClass == "ConsumerSpending").ToList();
            var consumerSpendingTotalYear = consumerSpendingYear.Sum(v => v.Amount);
            vm.ConsumerSpendingMealOrEatOutExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "MealOrEatOutExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingHousingOrSuppliesCostYear = consumerSpendingYear.Where(x => x.SubClass == "HousingOrSuppliesCost").Sum(v => v.Amount);
            vm.ConsumerSpendingEducationExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "EducationExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingMedicalExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "MedicalExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingTransportationCostYear = consumerSpendingYear.Where(x => x.SubClass == "TransportationCost").Sum(v => v.Amount);
            vm.ConsumerSpendingCommunicationCostYear = consumerSpendingYear.Where(x => x.SubClass == "CommunicationCost").Sum(v => v.Amount);
            vm.ConsumerSpendingLeisureOrCultureYear = consumerSpendingYear.Where(x => x.SubClass == "LeisureOrCulture").Sum(v => v.Amount);
            vm.ConsumerSpendingClothingOrShoesYear = consumerSpendingYear.Where(x => x.SubClass == "ClothingOrShoes").Sum(v => v.Amount);
            vm.ConsumerSpendingPinMoneyYear = consumerSpendingYear.Where(x => x.SubClass == "PinMoney").Sum(v => v.Amount);
            vm.ConsumerSpendingProtectionTypeInsuranceYear = consumerSpendingYear.Where(x => x.SubClass == "ProtectionTypeInsurance").Sum(v => v.Amount);
            vm.ConsumerSpendingOtherExpensesYear = consumerSpendingYear.Where(x => x.SubClass == "OtherExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingUnknownExpenditureYear = consumerSpendingYear.Where(x => x.SubClass == "UnknownExpenditure").Sum(v => v.Amount);
            if (consumerSpendingTotalYear != 0)
            {
                vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear = 100 * ((double)vm.ConsumerSpendingMealOrEatOutExpensesYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYear = 100 * ((double)vm.ConsumerSpendingHousingOrSuppliesCostYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingEducationExpensesYear = 100 * ((double)vm.ConsumerSpendingEducationExpensesYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingMedicalExpensesYear = 100 * ((double)vm.ConsumerSpendingMedicalExpensesYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingTransportationCostYear = 100 * ((double)vm.ConsumerSpendingTransportationCostYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingCommunicationCostYear = 100 * ((double)vm.ConsumerSpendingCommunicationCostYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingLeisureOrCultureYear = 100 * ((double)vm.ConsumerSpendingLeisureOrCultureYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingClothingOrShoesYear = 100 * ((double)vm.ConsumerSpendingClothingOrShoesYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingPinMoneyYear = 100 * ((double)vm.ConsumerSpendingPinMoneyYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYear = 100 * ((double)vm.ConsumerSpendingProtectionTypeInsuranceYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingOtherExpensesYear = 100 * ((double)vm.ConsumerSpendingOtherExpensesYear / (double)consumerSpendingTotalYear);
                vm.PercentageOfConsumerSpendingUnknownExpenditureYear = 100 * ((double)vm.ConsumerSpendingUnknownExpenditureYear / (double)consumerSpendingTotalYear);
            }

            // ---- YearMonth Income ----
            var regularIncomeYearMonth = vm.IncomeYearMonthOutputViewModels.Where(x => x.MainClass == "RegularIncome").ToList();
            var regularIncomeTotalYearMonth = regularIncomeYearMonth.Sum(v => v.Amount);
            vm.RegularIncomeLaborIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            vm.RegularIncomeBusinessIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "BusinessIncome").Sum(v => v.Amount);
            vm.RegularIncomePensionIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "PensionIncome").Sum(v => v.Amount);
            vm.RegularIncomeFinancialIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "FinancialIncome").Sum(v => v.Amount);
            vm.RegularIncomeRentalIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "RentalIncome").Sum(v => v.Amount);
            vm.RegularIncomeOtherIncomeYearMonth = regularIncomeYearMonth.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (regularIncomeTotalYearMonth != 0)
            {
                vm.PercentageOfRegularIncomeLaborIncomeYearMonth = 100 * ((double)vm.RegularIncomeLaborIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                vm.PercentageOfRegularIncomeBusinessIncomeYearMonth = 100 * ((double)vm.RegularIncomeBusinessIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                vm.PercentageOfRegularIncomePensionIncomeYearMonth = 100 * ((double)vm.RegularIncomePensionIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                vm.PercentageOfRegularIncomeFinancialIncomeYearMonth = 100 * ((double)vm.RegularIncomeFinancialIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                vm.PercentageOfRegularIncomeRentalIncomeYearMonth = 100 * ((double)vm.RegularIncomeRentalIncomeYearMonth / (double)regularIncomeTotalYearMonth);
                vm.PercentageOfRegularIncomeOtherIncomeYearMonth = 100 * ((double)vm.RegularIncomeOtherIncomeYearMonth / (double)regularIncomeTotalYearMonth);
            }

            var irregularIncomeYearMonth = vm.IncomeYearMonthOutputViewModels.Where(x => x.MainClass == "IrregularIncome").ToList();
            var irregularIncomeTotalYearMonth = irregularIncomeYearMonth.Sum(v => v.Amount);
            vm.IrregularIncomeLaborIncomeYearMonth = irregularIncomeYearMonth.Where(x => x.SubClass == "LaborIncome").Sum(v => v.Amount);
            vm.IrregularIncomeOtherIncomeYearMonth = irregularIncomeYearMonth.Where(x => x.SubClass == "OtherIncome").Sum(v => v.Amount);
            if (irregularIncomeTotalYearMonth != 0)
            {
                vm.PercentageOfIrregularIncomeLaborIncomeYearMonth = 100 * ((double)vm.IrregularIncomeLaborIncomeYearMonth / (double)irregularIncomeTotalYearMonth);
                vm.PercentageOfIrregularIncomeOtherIncomeYearMonth = 100 * ((double)vm.IrregularIncomeOtherIncomeYearMonth / (double)irregularIncomeTotalYearMonth);
            }

            // ---- YearMonth Expenditure ----
            var regularSavingsYearMonth = vm.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "RegularSavings").ToList();
            var regularSavingsTotalYearMonth = regularSavingsYearMonth.Sum(v => v.Amount);
            vm.RegularSavingsDepositYearMonth = regularSavingsYearMonth.Where(x => x.SubClass == "Deposit").Sum(v => v.Amount);
            vm.RegularSavingsInvestmentYearMonth = regularSavingsYearMonth.Where(x => x.SubClass == "Investment").Sum(v => v.Amount);
            if (regularSavingsTotalYearMonth != 0)
            {
                vm.PercentageOfRegularSavingsDepositYearMonth = 100 * ((double)vm.RegularSavingsDepositYearMonth / (double)regularSavingsTotalYearMonth);
                vm.PercentageOfRegularSavingsInvestmentYearMonth = 100 * ((double)vm.RegularSavingsInvestmentYearMonth / (double)regularSavingsTotalYearMonth);
            }

            var nonConsumerSpendingYearMonth = vm.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "NonConsumerSpending").ToList();
            var nonConsumerSpendingTotalYearMonth = nonConsumerSpendingYearMonth.Sum(v => v.Amount);
            vm.NonConsumerSpendingPublicPensionYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "PublicPension").Sum(v => v.Amount);
            vm.NonConsumerSpendingDebtRepaymentYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "DebtRepayment").Sum(v => v.Amount);
            vm.NonConsumerSpendingTaxYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "Tax").Sum(v => v.Amount);
            vm.NonConsumerSpendingSocialInsuranceYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "SocialInsurance").Sum(v => v.Amount);
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "InterHouseholdTranserExpenses").Sum(v => v.Amount);
            vm.NonConsumerSpendingNonProfitOrganizationTransferYearMonth = nonConsumerSpendingYearMonth.Where(x => x.SubClass == "NonProfitOrganizationTransfer").Sum(v => v.Amount);
            if (nonConsumerSpendingTotalYearMonth != 0)
            {
                vm.PercentageOfNonConsumerSpendingPublicPensionYearMonth = 100 * ((double)vm.NonConsumerSpendingPublicPensionYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                vm.PercentageOfNonConsumerSpendingDebtRepaymentYearMonth = 100 * ((double)vm.NonConsumerSpendingDebtRepaymentYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                vm.PercentageOfNonConsumerSpendingTaxYearMonth = 100 * ((double)vm.NonConsumerSpendingTaxYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                vm.PercentageOfNonConsumerSpendingSocialInsuranceYearMonth = 100 * ((double)vm.NonConsumerSpendingSocialInsuranceYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth = 100 * ((double)vm.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth / (double)nonConsumerSpendingTotalYearMonth);
                vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth = 100 * ((double)vm.NonConsumerSpendingNonProfitOrganizationTransferYearMonth / (double)nonConsumerSpendingTotalYearMonth);
            }

            var consumerSpendingYearMonth = vm.ExpenditureYearMonthOutputViewModels.Where(x => x.MainClass == "ConsumerSpending").ToList();
            var consumerSpendingTotalYearMonth = consumerSpendingYearMonth.Sum(v => v.Amount);
            vm.ConsumerSpendingMealOrEatOutExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "MealOrEatOutExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingHousingOrSuppliesCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "HousingOrSuppliesCost").Sum(v => v.Amount);
            vm.ConsumerSpendingEducationExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "EducationExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingMedicalExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "MedicalExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingTransportationCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "TransportationCost").Sum(v => v.Amount);
            vm.ConsumerSpendingCommunicationCostYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "CommunicationCost").Sum(v => v.Amount);
            vm.ConsumerSpendingLeisureOrCultureYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "LeisureOrCulture").Sum(v => v.Amount);
            vm.ConsumerSpendingClothingOrShoesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "ClothingOrShoes").Sum(v => v.Amount);
            vm.ConsumerSpendingPinMoneyYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "PinMoney").Sum(v => v.Amount);
            vm.ConsumerSpendingProtectionTypeInsuranceYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "ProtectionTypeInsurance").Sum(v => v.Amount);
            vm.ConsumerSpendingOtherExpensesYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "OtherExpenses").Sum(v => v.Amount);
            vm.ConsumerSpendingUnknownExpenditureYearMonth = consumerSpendingYearMonth.Where(x => x.SubClass == "UnknownExpenditure").Sum(v => v.Amount);
            if (consumerSpendingTotalYearMonth != 0)
            {
                vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth = 100 * ((double)vm.ConsumerSpendingMealOrEatOutExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth = 100 * ((double)vm.ConsumerSpendingHousingOrSuppliesCostYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingEducationExpensesYearMonth = 100 * ((double)vm.ConsumerSpendingEducationExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingMedicalExpensesYearMonth = 100 * ((double)vm.ConsumerSpendingMedicalExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingTransportationCostYearMonth = 100 * ((double)vm.ConsumerSpendingTransportationCostYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingCommunicationCostYearMonth = 100 * ((double)vm.ConsumerSpendingCommunicationCostYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingLeisureOrCultureYearMonth = 100 * ((double)vm.ConsumerSpendingLeisureOrCultureYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingClothingOrShoesYearMonth = 100 * ((double)vm.ConsumerSpendingClothingOrShoesYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingPinMoneyYearMonth = 100 * ((double)vm.ConsumerSpendingPinMoneyYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth = 100 * ((double)vm.ConsumerSpendingProtectionTypeInsuranceYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingOtherExpensesYearMonth = 100 * ((double)vm.ConsumerSpendingOtherExpensesYearMonth / (double)consumerSpendingTotalYearMonth);
                vm.PercentageOfConsumerSpendingUnknownExpenditureYearMonth = 100 * ((double)vm.ConsumerSpendingUnknownExpenditureYearMonth / (double)consumerSpendingTotalYearMonth);
            }
        }

        return View(vm);
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
            string email = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte ?? [])).Email;
            await dashboardService.UpdateDefaultMonetaryUnitAsync(email, userIndexInputViewModel.DefaultMonetaryUnit);
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
