using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class AdmissionTicketOutputModel
    {
        public string RowNumber { get; set; }
        public string EntranceGate { get; set; }
        public int TotalMemberNum { get; set; }
        public string Category { get; set; }
        public string CoachName { get; set; }
        public int TeamNo { get; set; }
        public string CoachEmail { get; set; }
    }
}
