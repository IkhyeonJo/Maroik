using System.ComponentModel.DataAnnotations;

namespace Maroik.Website.Tests.Grids.Fixtures;

public enum TestEnum
{
    [Display(Name = "1st")]
    First,

    [Display(Name = "2nd")]
    Second,

    [Display(Name = null)]
    Third,

    Fourth
}
