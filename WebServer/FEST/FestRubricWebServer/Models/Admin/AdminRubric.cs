using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class AdminRubric
    {
        public int BeforeTeamNumber { get; set; }
        public int TeamNumber { get; set; }
        public string Evaluator { get; set; }
        public string Type { get; set; }
        public int CategoryOne0 { get; set; }
        public int CategoryOne1 { get; set; }
        public int CategoryOne2 { get; set; }
        public int CategoryTwo0 { get; set; }
        public int CategoryTwo1 { get; set; }
        public int CategoryTwo2 { get; set; }
        public int CategoryThree0 { get; set; }
        public int CategoryThree1 { get; set; }
        public int CategoryThree2 { get; set; }
        public int CategoryScore { get; set; }
        public string GoodText { get; set; }
        public string Opinion { get; set; }
        public string Reserved0 { get; set; }
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }
    }
}
