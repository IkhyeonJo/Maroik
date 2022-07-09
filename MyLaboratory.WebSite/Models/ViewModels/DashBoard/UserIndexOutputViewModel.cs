using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MyLaboratory.Common.DataAccess.Models;

namespace MyLaboratory.WebSite.Models.ViewModels.DashBoard
{
    public class UserIndexOutputViewModel
    {
        public IEnumerable<Income> Incomes { get; set; }
        public IEnumerable<Expenditure> Expenditures { get; set; }
    }
}