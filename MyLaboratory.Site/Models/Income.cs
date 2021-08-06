using System;
using System.Collections.Generic;

#nullable disable

namespace MyLaboratory.Site.Models
{
    public partial class Income
    {
        public int Id { get; set; }
        public string AccountEmail { get; set; }
        public string MainClass { get; set; }
        public string SubClass { get; set; }
        public string Contents { get; set; }
        public long Amount { get; set; }
        public string MonetaryUnit { get; set; }
        public string DepositAccount { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Note { get; set; }
        public string Reserved0 { get; set; }
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public virtual Asset Asset { get; set; }
    }
}
