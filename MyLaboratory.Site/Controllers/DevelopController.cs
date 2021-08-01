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
    public class DevelopController : Controller
    {
        private readonly ILogger<DevelopController> logger;

        public DevelopController(ILogger<DevelopController> logger)
        {
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