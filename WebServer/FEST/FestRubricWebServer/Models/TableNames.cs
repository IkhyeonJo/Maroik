using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models
{
    public static class TableNames
    {
        #region 2018년도 FEST
        //public static string Member { get; }
        //public static string TeamParticipation { get; }
        //public static string Users { get; }
        #endregion

        #region 2019년도 FEST
        public static string Member { get; }
        public static string TeamParticipation { get; }
        public static string Users { get; }
        public static string Login { get; set; }
        #endregion
    }
}
