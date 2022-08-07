using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.Blog.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Blog.Services;
using Microsoft.AspNetCore.Authorization;
using MyLaboratory.Blog.Common;
using MyLaboratory.Blog.Contracts;
using MyLaboratory.Blog.Helpers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;

namespace MyLaboratory.Blog.Controllers
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