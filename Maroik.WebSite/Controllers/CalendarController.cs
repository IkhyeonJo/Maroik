using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class CalendarController : Controller
    {
        private readonly ILogger<CalendarController> _logger;
        private readonly IIncomeRepository _incomeRepository;
        private readonly IExpenditureRepository _expenditureRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;

        public CalendarController(ILogger<CalendarController> logger, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IAssetRepository assetRepository, IAccountRepository accountRepository)
        {
            _logger = logger;
            _incomeRepository = incomeRepository;
            _expenditureRepository = expenditureRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
        }

        #region Admin
        [HttpGet]
        public IActionResult AdminIndex()
        {
            return View();
        }
        #endregion

        #region User

        #region Read
        [HttpGet]
        public IActionResult UserIndex()
        {
            return View();
        }
        #endregion

        #endregion

        #region Anonymous
        [HttpGet]
        public IActionResult AnonymousIndex()
        {
            return View();
        }
        #endregion
    }
}
