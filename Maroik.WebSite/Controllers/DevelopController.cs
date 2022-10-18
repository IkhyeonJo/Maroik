using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Maroik.WebSite.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using Maroik.WebSite.Services;
using Microsoft.AspNetCore.Authorization;
using Maroik.WebSite.Common;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Helpers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.WebSite.Controllers
{
    public class DevelopController : Controller
    {
        private readonly IHtmlLocalizer<DevelopController> localizer;
        private readonly ILogger<DevelopController> logger;

        public DevelopController(IHtmlLocalizer<DevelopController> localizer, ILogger<DevelopController> logger)
        {
            this.localizer = localizer;
            this.logger = logger;
        }

        #region WebAPI

        #region Read
        [HttpGet]
        public IActionResult API()
        {
            return View();
        }
        #endregion

        #endregion
    }
}