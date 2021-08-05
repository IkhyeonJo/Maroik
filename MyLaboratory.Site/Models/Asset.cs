using System;
using System.Collections.Generic;

#nullable disable

namespace MyLaboratory.Site.Models
{
    public partial class Asset
    {
        public Asset()
        {
            Expenditures = new HashSet<Expenditure>();
            Incomes = new HashSet<Income>();
            Liabilities = new HashSet<Liability>();
        }

        public string ProductName { get; set; }
        public string AccountEmail { get; set; }
        public string Item { get; set; }
        public long Amount { get; set; }
        public string MonetaryUnit { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Note { get; set; }
        public bool Deleted { get; set; }
        public string Reserved0 { get; set; }
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public virtual Account AccountEmailNavigation { get; set; }
        public virtual ICollection<Expenditure> Expenditures { get; set; }
        public virtual ICollection<Income> Incomes { get; set; }
        public virtual ICollection<Liability> Liabilities { get; set; }
    }
}
