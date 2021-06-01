using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public static class TempLoginInputModel
    {
        public static int TeamNo { get; set; }
        public static string CoachEmail { get; set; }
        public static string CoachPassword { get; set; }
    }
}
