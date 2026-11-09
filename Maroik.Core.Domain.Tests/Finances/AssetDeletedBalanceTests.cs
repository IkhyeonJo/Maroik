using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>A deleted asset's balance never changes, whatever a caller checked or forgot to check first.</summary>
public class AssetDeletedBalanceTests
{
    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A soft-deleted asset holding 1000 KRW.</summary>
    private static Asset DeletedAsset()
    {
        Asset asset = Asset.Create("My Bank", "user@example.com", "FreeDepositAndWithdrawal", 1000m, "KRW", _now).Value;
        asset.SoftDelete(_now);
        return asset;
    }

    /// <summary>A deposit is refused and leaves the balance as it was.</summary>
    [Fact]
    public void Deposit_IsRefused_OnADeletedAsset()
    {
        Asset asset = DeletedAsset();

        var result = asset.Deposit(Money.Create(1m, "KRW").Value, _now);

        Assert.Equal("Asset.Deleted", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }

    /// <summary>A withdrawal is refused and leaves the balance as it was.</summary>
    [Fact]
    public void Withdraw_IsRefused_OnADeletedAsset()
    {
        Asset asset = DeletedAsset();

        var result = asset.Withdraw(Money.Create(1m, "KRW").Value, _now);

        Assert.Equal("Asset.Deleted", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }
}
