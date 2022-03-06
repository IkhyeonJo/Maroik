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
using Microsoft.ML;
using MyLaboratory.WebSite.Models.ViewModels.DashBoard;
using Microsoft.ML.Transforms.TimeSeries;

namespace MyLaboratory.WebSite.Controllers
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
            var context = new MLContext();

            var data = context.Data.LoadFromTextFile<AdminIndexDataOutputViewModel>("./wwwroot/admin/custom/DashBoard/AdminIndex/data/diaper_panty_sales.csv",
                hasHeader: true, separatorChar: ',');

            var pipeline = context.Forecasting.ForecastBySsa(
                nameof(AdminIndexForecastOutputViewModel.Forecast),
                nameof(AdminIndexDataOutputViewModel.TotalSales),
                windowSize: 5,
                seriesLength: 10,
                trainSize: 578,
                horizon: 7); // 모든 데이터 학습 후 1주일 치 예측

            var model = pipeline.Fit(data);

            var forecastingEngine = model.CreateTimeSeriesEngine<AdminIndexDataOutputViewModel, AdminIndexForecastOutputViewModel>(context);

            var forecasts = forecastingEngine.Predict();

            //foreach (var forecast in forecasts.Forecast)
            //{
            //    Console.WriteLine(forecast);
            //}

            ViewBag.ForecastResult = forecasts.Forecast;

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
