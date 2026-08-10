using Maroik.Core.Domain.Finance;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="IncomeClassPolicy"/>.
/// Covers all valid main/sub-class combinations, invalid sub-classes per main class,
/// and unknown main classes including null and empty inputs.
/// </summary>
public class IncomeClassPolicyTests
{
    // -- RegularIncome ---------------------------------------------------------

    /// <summary>Validate returns success for all regular income sub classes.</summary>
    [Theory]
    [InlineData("LaborIncome")]
    [InlineData("BusinessIncome")]
    [InlineData("PensionIncome")]
    [InlineData("FinancialIncome")]
    [InlineData("RentalIncome")]
    [InlineData("OtherIncome")]
    public void Validate_ReturnsSuccess_ForAllRegularIncomeSubClasses(string subClass)
    {
        var result = IncomeClassPolicy.Validate("RegularIncome", subClass);

        Assert.False(result.IsError);
    }

    /// <summary>Validate returns error, when regular income sub class invalid.</summary>
    [Theory]
    [InlineData("UnknownSub")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenRegularIncomeSubClassInvalid(string? subClass)
    {
        var result = IncomeClassPolicy.Validate("RegularIncome", subClass);

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }

    // -- IrregularIncome -------------------------------------------------------

    /// <summary>Validate returns success for all irregular income sub classes.</summary>
    [Theory]
    [InlineData("LaborIncome")]
    [InlineData("OtherIncome")]
    public void Validate_ReturnsSuccess_ForAllIrregularIncomeSubClasses(string subClass)
    {
        var result = IncomeClassPolicy.Validate("IrregularIncome", subClass);

        Assert.False(result.IsError);
    }

    /// <summary>Validate returns error, when irregular income sub class invalid.</summary>
    [Theory]
    [InlineData("BusinessIncome")]
    [InlineData("PensionIncome")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenIrregularIncomeSubClassInvalid(string? subClass)
    {
        var result = IncomeClassPolicy.Validate("IrregularIncome", subClass);

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }

    // -- Unknown main class ----------------------------------------------------

    /// <summary>Validate returns error, when main class unknown.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenMainClassUnknown(string? mainClass)
    {
        var result = IncomeClassPolicy.Validate(mainClass, "LaborIncome");

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }
}
