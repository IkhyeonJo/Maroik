using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.WebSite.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.WebSite.Services;
using Microsoft.AspNetCore.Authorization;
using MyLaboratory.WebSite.Common;
using MyLaboratory.WebSite.Contracts;
using MyLaboratory.Common.DataAccess.Contracts;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MyLaboratory.Common.DataAccess.Models;
using System.Text;

namespace MyLaboratory.WebSite.Controllers
{
    public class DashBoardController : Controller
    {
        private readonly ILogger<DashBoardController> logger;
        private readonly IIncomeRepository incomeRepository;

        public DashBoardController(ILogger<DashBoardController> logger, IIncomeRepository incomeRepository)
        {
            this.logger = logger;
            this.incomeRepository = incomeRepository;
        }

        #region Admin
        [HttpGet]
        public IActionResult AdminIndex()
        {
            return View();
        }
        #endregion

        #region User
        [HttpGet]
        public async Task<IActionResult> UserIndex()
        {
            HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
            #region Current Year & Month Income
            var currentYearMonth = DateTime.Now.ToString("yyyy-MM");
            var currentLoginUserEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;

            var tempCurrentYearMonthIncomes = await incomeRepository.GetCurrentYearMonthIncomesAsync(currentLoginUserEmail, currentYearMonth);
            #endregion
            return View();
        }
        #endregion

    }
}
