using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.Site.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Services;
using Microsoft.AspNetCore.Authorization;
using MyLaboratory.Site.Common;
using MyLaboratory.Site.Contracts;

namespace MyLaboratory.Site.Controllers
{
    public class DashBoardController : Controller
    {
        private readonly ILogger<DashBoardController> logger;

        public DashBoardController(ILogger<DashBoardController> logger)
        {
            this.logger = logger;
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
        public IActionResult UserIndex()
        {
            return View();
        }
        #endregion

    }
}
