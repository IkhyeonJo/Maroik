using Fingers10.ExcelExport.Attributes;
using JqueryDataTables.ServerSide.AspNetCoreWeb.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyLaboratory.Site.Models
{
    public class Demo
    {
        public Demo()
        {
            DemoNestedLevelOne = new DemoNestedLevelOne();
        }

        [JqueryDataTableColumn(Order = 1)]
        public int Id { get; set; }

        [IncludeInReport(Order = 1)]
        [JqueryDataTableColumn(Order = 2)]
        [SearchableString(EntityProperty = "FirstName,LastName")]
        [Sortable(EntityProperty = "FirstName,LastName", Default = true)]
        public string Name { get; set; }

        [JqueryDataTableColumn(Exclude = true)]
        public string FirstName { get; set; }

        [JqueryDataTableColumn(Exclude = true)]
        public string LastName { get; set; }

        [IncludeInReport(Order = 2)]
        [JqueryDataTableColumn(Order = 3)]
        [SearchableEnum(typeof(Position))]
        [Sortable]
        public string Position { get; set; }

        [Display(Name = "Office")]
        [IncludeInReport(Order = 3)]
        [JqueryDataTableColumn(Order = 4)]
        [SearchableString(EntityProperty = "Office")]
        [Sortable(EntityProperty = "Office")]
        public string Offices { get; set; }

        [NestedIncludeInReport]
        [NestedSearchable]
        [NestedSortable]
        public DemoNestedLevelOne DemoNestedLevelOne { get; set; }
    }
}
