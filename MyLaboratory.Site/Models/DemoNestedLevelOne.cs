using Fingers10.ExcelExport.Attributes;
using JqueryDataTables.ServerSide.AspNetCoreWeb.Attributes;
using System.ComponentModel;

namespace MyLaboratory.Site.Models
{
    public class DemoNestedLevelOne
    {
        public DemoNestedLevelOne()
        {
            DemoNestedLevelTwos = new DemoNestedLevelTwo();
        }

        [IncludeInReport(Order = 4)]
        [JqueryDataTableColumn(Order = 5)]
        [SearchableShort]
        [Sortable]
        public short? Experience { get; set; }

        [DisplayName("Extn")]
        [IncludeInReport(Order = 5)]
        [JqueryDataTableColumn(Order = 6)]
        [SearchableInt(EntityProperty = "Extn")]
        [Sortable(EntityProperty = "Extn")]
        public int? Extension { get; set; }

        [NestedIncludeInReport]
        [NestedSearchable(ParentEntityProperty = "DemoNestedLevelTwo")]
        [NestedSortable(ParentEntityProperty = "DemoNestedLevelTwo")]
        public DemoNestedLevelTwo DemoNestedLevelTwos { get; set; }
    }
}
