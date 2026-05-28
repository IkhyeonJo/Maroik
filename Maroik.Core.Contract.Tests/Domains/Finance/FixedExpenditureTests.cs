using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Tests.Domains.Finance;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Domains.Finance.FixedExpenditure"/>.
/// Covers registration validation (deposit day/month range, payment method), unpunctuality flag management, and update logic.
/// </summary>
public class FixedExpenditureTests
{
    private static readonly DateTime Maturity = DateTime.UtcNow.AddYears(1);

    private static FixedExpenditure ValidFixedExpenditure() =>
        FixedExpenditure.Register("user@example.com", "ConsumerSpending", "SubscriptionService", "Netflix",
            15000m, "KRW", "My Card", null, 0, 1, Maturity).Value;

    // -- Register -------------------------------------------------------------

    [Fact]
    public void Register_ReturnsFixedExpenditure_WhenValid()
    {
        var result = FixedExpenditure.Register("user@example.com", "ConsumerSpending", "SubscriptionService", "Netflix",
            15000m, "KRW", "My Card", null, 0, 1, Maturity);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.DepositDay);
        Assert.Equal("My Card", result.Value.PaymentMethod);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Register_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var result = FixedExpenditure.Register("user@example.com", "ConsumerSpending", "SubscriptionService", null,
            0m, "KRW", "My Card", null, 0, day, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedExpenditure.InvalidDepositDay", result.FirstError.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    public void Register_ReturnsError_WhenDepositMonthOutOfRange(short month)
    {
        var result = FixedExpenditure.Register("user@example.com", "ConsumerSpending", "SubscriptionService", null,
            0m, "KRW", "My Card", null, month, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedExpenditure.InvalidDepositMonth", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_ReturnsError_WhenPaymentMethodEmpty(string? method)
    {
        var result = FixedExpenditure.Register("user@example.com", "ConsumerSpending", "SubscriptionService", null,
            0m, "KRW", method, null, 0, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedExpenditure.PaymentMethodEmpty", result.FirstError.Code);
    }

    // -- MarkUnpunctual / ClearUnpunctuality -----------------------------------

    [Fact]
    public void MarkUnpunctual_SetsFlag()
    {
        var fe = ValidFixedExpenditure();

        fe.MarkUnpunctual();

        Assert.True(fe.Unpunctuality);
    }

    [Fact]
    public void ClearUnpunctuality_ClearsFlag()
    {
        var fe = ValidFixedExpenditure();
        fe.MarkUnpunctual();

        fe.ClearUnpunctuality();

        Assert.False(fe.Unpunctuality);
    }

    // -- Update ----------------------------------------------------------------

    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var fe = ValidFixedExpenditure();

        var result = fe.Update("ConsumerSpending", "InsurancePremium", "Car insurance", 80000m, "KRW", "Bank Account", null, 0, 15, Maturity, "note");

        Assert.False(result.IsError);
        Assert.Equal("InsurancePremium", fe.SubClass);
        Assert.Equal(15, fe.DepositDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Update_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var fe = ValidFixedExpenditure();

        var result = fe.Update("ConsumerSpending", "SubscriptionService", null, 0m, "KRW", "My Card", null, 0, day, Maturity, null);

        Assert.True(result.IsError);
        Assert.Equal("FixedExpenditure.InvalidDepositDay", result.FirstError.Code);
    }
}
