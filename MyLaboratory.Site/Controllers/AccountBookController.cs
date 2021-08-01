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
    public class AccountBookController : Controller
    {
        private readonly ILogger<AccountBookController> logger;

        public AccountBookController(ILogger<AccountBookController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Main()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Chart()
        {
            return View();
        }
    }
}
