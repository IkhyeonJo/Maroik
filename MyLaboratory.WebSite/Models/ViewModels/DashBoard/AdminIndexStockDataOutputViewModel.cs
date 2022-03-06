using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.ML.Data;

namespace MyLaboratory.WebSite.Models.ViewModels.DashBoard
{
    public class AdminIndexStockDataOutputViewModel
    {
        [LoadColumn(0)]
        public DateTime Date { get; set; }

        [LoadColumn(1)]
        public float TotalStock { get; set; }
    }
}