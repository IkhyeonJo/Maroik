using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class CoachOutputModel
    {
        public string TeamOrganization { get; set; }
        public string CoachName { get; set; }
        public string CoachPhone { get; set; }
        public string CoachEmail { get; set; }


        public int TeamNo { get; set; }
    }
}
