using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Constants;

/// <summary>The income class names, as the finance breakdowns key them, for the code outside the Service layer; each is the name of the Domain's own enum member.</summary>
public static class IncomeClassNames
{
    /// <summary><see cref="IncomeMainClass.RegularIncome"/>.</summary>
    public const string RegularIncome = nameof(IncomeMainClass.RegularIncome);

    /// <summary><see cref="IncomeMainClass.IrregularIncome"/>.</summary>
    public const string IrregularIncome = nameof(IncomeMainClass.IrregularIncome);

    /// <summary><see cref="IncomeSubClass.LaborIncome"/>.</summary>
    public const string LaborIncome = nameof(IncomeSubClass.LaborIncome);

    /// <summary><see cref="IncomeSubClass.BusinessIncome"/>.</summary>
    public const string BusinessIncome = nameof(IncomeSubClass.BusinessIncome);

    /// <summary><see cref="IncomeSubClass.PensionIncome"/>.</summary>
    public const string PensionIncome = nameof(IncomeSubClass.PensionIncome);

    /// <summary><see cref="IncomeSubClass.FinancialIncome"/>.</summary>
    public const string FinancialIncome = nameof(IncomeSubClass.FinancialIncome);

    /// <summary><see cref="IncomeSubClass.RentalIncome"/>.</summary>
    public const string RentalIncome = nameof(IncomeSubClass.RentalIncome);

    /// <summary><see cref="IncomeSubClass.OtherIncome"/>.</summary>
    public const string OtherIncome = nameof(IncomeSubClass.OtherIncome);
}
