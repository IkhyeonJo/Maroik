using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Maroik.Common.DataAccess.Models;

namespace Maroik.WebSite.Models.ViewModels.DashBoard
{
    public class UserIndexOutputViewModel
    {
        public IEnumerable<Income> Incomes { get; set; } = new List<Income>();
        public IEnumerable<Expenditure> Expenditures { get; set; } = new List<Expenditure>();
    }
}