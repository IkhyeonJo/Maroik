using Fingers10.ExcelExport.Attributes;
using JqueryDataTables.ServerSide.AspNetCoreWeb.Attributes;
using System;
using System.ComponentModel;

namespace MyLaboratory.Site.Models
{
    public class DemoNestedLevelTwo
    {
        [DisplayName("Start Date")]
        [IncludeInReport(Order = 6)]
        [JqueryDataTableColumn(Order = 7)]
        [SearchableDateTime(EntityProperty = "StartDate")]
        [Sortable(EntityProperty = "StartDate")]
        public DateTime? StartDates { get; set; }

        [IncludeInReport(Order = 7)]
        [JqueryDataTableColumn(Order = 8)]
        [SearchableLong]
        [Sortable]
        public long? Salary { get; set; }

        [JqueryDataTableColumn(Order = 9)]
        public string Action { get; set; }
    }
}
