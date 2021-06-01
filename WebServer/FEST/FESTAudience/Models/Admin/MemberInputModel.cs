using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class MemberInputModel
    {
        [Key]
        public int MemberID { get; set; }
        public string MemberName { get; set; }
        public string ResidentNumber { get; set; }
        public string School { get; set; }
        public string Level { get; set; }
        public string Grade { get; set; }
        public string MemberPhone { get; set; }
        public string MemberEmail { get; set; }
        public string Category { get; set; }
        public string Year { get; set; }
        public int TeamNo { get; set; }
        public string GuardianName1 { get; set; }
        public string GuardianPhone1 { get; set; }
        public string Relationship1 { get; set; }
    }
}
