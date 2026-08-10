using Maroik.Core.Domain.Finance;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="ExpenditureClassPolicy"/>.
/// Covers all valid main/sub-class combinations (with and without deposit-asset requirement),
/// invalid sub-classes per main class, and unknown main classes including null and empty inputs.
/// </summary>
public class ExpenditureClassPolicyTests
{
    // -- RegularSavings --------------------------------------------------------

    /// <summary>Validate returns true for regular savings sub classes.</summary>
    [Theory]
    [InlineData("Deposit")]
    [InlineData("Investment")]
    public void Validate_ReturnsTrue_ForRegularSavingsSubClasses(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("RegularSavings", subClass);

        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    /// <summary>Validate returns error, when regular savings sub class invalid.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenRegularSavingsSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("RegularSavings", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- NonConsumerSpending ---------------------------------------------------

    /// <summary>Validate returns true for non consumer spending with deposit asset.</summary>
    [Theory]
    [InlineData("PublicPension")]
    [InlineData("DebtRepayment")]
    public void Validate_ReturnsTrue_ForNonConsumerSpendingWithDepositAsset(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    /// <summary>Validate returns false for non consumer spending without deposit asset.</summary>
    [Theory]
    [InlineData("Tax")]
    [InlineData("SocialInsurance")]
    [InlineData("InterHouseholdTranserExpenses")]
    [InlineData("NonProfitOrganizationTransfer")]
    public void Validate_ReturnsFalse_ForNonConsumerSpendingWithoutDepositAsset(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }

    /// <summary>Validate returns error, when non consumer spending sub class invalid.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenNonConsumerSpendingSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- ConsumerSpending ------------------------------------------------------

    /// <summary>Validate returns false for all consumer spending sub classes.</summary>
    [Theory]
    [InlineData("MealOrEatOutExpenses")]
    [InlineData("HousingOrSuppliesCost")]
    [InlineData("EducationExpenses")]
    [InlineData("MedicalExpenses")]
    [InlineData("TransportationCost")]
    [InlineData("CommunicationCost")]
    [InlineData("LeisureOrCulture")]
    [InlineData("ClothingOrShoes")]
    [InlineData("PinMoney")]
    [InlineData("ProtectionTypeInsurance")]
    [InlineData("OtherExpenses")]
    [InlineData("UnknownExpenditure")]
    public void Validate_ReturnsFalse_ForAllConsumerSpendingSubClasses(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("ConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }

    /// <summary>Validate returns error, when consumer spending sub class invalid.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenConsumerSpendingSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("ConsumerSpending", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- Unknown main class ----------------------------------------------------

    /// <summary>Validate returns error, when main class unknown.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenMainClassUnknown(string? mainClass)
    {
        var result = ExpenditureClassPolicy.Validate(mainClass, "Tax");

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }
}
