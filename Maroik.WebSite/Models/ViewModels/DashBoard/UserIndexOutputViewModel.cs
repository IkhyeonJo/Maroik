using Maroik.Common.DataAccess.Models;

namespace Maroik.WebSite.Models.ViewModels.DashBoard
{
    public class UserIndexOutputViewModel
    {
        public IEnumerable<Income> Incomes { get; set; } = [];
        public IEnumerable<Expenditure> Expenditures { get; set; } = [];
        public IEnumerable<USP_Income_MonthlyReport> USP_Income_MonthlyReport_Results { get; set; } = [];
        public IEnumerable<USP_Expenditure_MonthlyReport> USP_Expenditure_MonthlyReport_Results { get; set; } = [];
        public IEnumerable<string> MonetaryUnits { get; set; } = [];
        public string DefaultMonetaryUnit { get; set; }
    }
}