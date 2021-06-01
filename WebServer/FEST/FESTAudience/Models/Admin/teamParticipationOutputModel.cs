using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class TeamParticipationOutputModel
    {
        public string Category { get; set; }
        public string Year { get; set; }
        public int TeamNo { get; set; }
        public string TeamName { get; set; }
        public string TeamOrganization { get; set; }
        public string CoachName { get; set; }
        public string CoachPhone { get; set; }
        public string CoachEmail { get; set; }
        public string CoachPassword { get; set; }
        public string KRC { get; set; }
        public bool Checkbox { get; set; }
    }
}
