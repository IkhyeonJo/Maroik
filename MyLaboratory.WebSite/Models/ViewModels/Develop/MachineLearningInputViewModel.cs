using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.WebSite.Models.ViewModels.Develop
{
    public class MachineLearningInputViewModel
    {
        public List<IFormFile> MLModelDataFiles { get; set; }
    }
}