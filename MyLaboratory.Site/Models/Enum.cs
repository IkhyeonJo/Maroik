using System.ComponentModel.DataAnnotations;

namespace MyLaboratory.Site.Models
{
    public enum Position
    {
        [Display(Name = "Accountant")]
        Accountant,
        [Display(Name = "Chief Executive Officer (CEO)")]
        ChiefExecutiveOfficer,
        [Display(Name = "Integration Specialist")]
        IntegrationSpecialist,
        [Display(Name = "Junior Technical Author")]
        JuniorTechnicalAuthor,
        [Display(Name = "Pre Sales Support")]
        PreSalesSupport,
        [Display(Name = "Sales Assistant")]
        SalesAssistant,
        [Display(Name = "Senior Javascript Developer")]
        SeniorJavascriptDeveloper,
        [Display(Name = "Software Engineer")]
        SoftwareEngineer
    }
}
