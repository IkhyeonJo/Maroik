using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Maroik.Common.DataAccess.Models;

namespace Maroik.WebSite.Models.ViewModels.Forum
{
    public class FreeForumOutputViewModel
    {
        public Pager Pager { get; set; }

        public IEnumerable<Board> Boards { get; set; } = new List<Board>();
    }
}