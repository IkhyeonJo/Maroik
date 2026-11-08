using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Tests.ValueObjects;

/// <summary>Unit tests for <see cref="AssetItem"/>, an asset's validated category.</summary>
public class AssetItemTests
{
    /// <summary>A known category is kept as it is.</summary>
    [Fact]
    public void Create_KeepsTheValue_WhenTheCategoryIsKnown()
    {
        var result = AssetItem.Create("CashAsset");

        Assert.False(result.IsError);
        Assert.Equal("CashAsset", result.Value.Value);
        Assert.Equal("CashAsset", result.Value.ToString());
    }

    /// <summary>A missing category is refused as empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ReturnsItemEmpty_WhenTheCategoryIsMissing(string? value)
    {
        Assert.Equal("Asset.ItemEmpty", AssetItem.Create(value).FirstError.Code);
    }

    /// <summary>A category outside the taxonomy is refused as invalid.</summary>
    [Fact]
    public void Create_ReturnsItemInvalid_WhenTheCategoryIsUnknown()
    {
        Assert.Equal("Asset.ItemInvalid", AssetItem.Create("Stock").FirstError.Code);
    }

    /// <summary>Two items of one category are the same value; two categories are not.</summary>
    [Fact]
    public void Equality_FollowsTheCategory()
    {
        Assert.Equal(AssetItem.Create("CashAsset").Value, AssetItem.Create("CashAsset").Value);
        Assert.NotEqual(AssetItem.Create("CashAsset").Value, AssetItem.Create("SavingsAsset").Value);
    }
}
