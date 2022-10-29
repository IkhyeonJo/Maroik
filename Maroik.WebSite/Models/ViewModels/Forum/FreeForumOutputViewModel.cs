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
        public string Method { get; set; }

        public Pager Pager { get; set; }

        public IEnumerable<Board> Boards { get; set; } = new List<Board>();

        public BoardOutputViewModel BoardOutputViewModel { get; set; }

        public int DetailBoardId { get; set; }

        public int DetailCurrentPage { get; set; }
    }
}