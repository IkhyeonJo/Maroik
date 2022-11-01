using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.Forum
{
    public class CommentInputViewModel
    {
        public int BoardId { get; set; }

        public string Content { get; set; }

        public int DetailCurrentPage { get; set; }
    }
}
