namespace Maroik.Core.Contract.Dtos;

/// <summary>What the finance forms (asset, income, expenditure, fixed income, fixed expenditure) offer and mirror; authoritative in the Domain.</summary>
public sealed class FinanceFormRules
{
    /// <summary>The income main classes, in display order.</summary>
    public IReadOnlyList<string> IncomeMainClasses { get; init; } = [];

    /// <summary>The income subclasses of each main class, in display order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> IncomeSubClassesByMainClass { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The expenditure main classes, in display order.</summary>
    public IReadOnlyList<string> ExpenditureMainClasses { get; init; } = [];

    /// <summary>The expenditure subclasses of each main class, in display order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ExpenditureSubClassesByMainClass { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The transfer-type expenditure subclasses, which need a deposit asset.</summary>
    public IReadOnlyList<string> DepositAssetSubClasses { get; init; } = [];

    /// <summary>The last deposit day each month allows.</summary>
    public IReadOnlyDictionary<int, int> MaxDepositDayByMonth { get; init; } = new Dictionary<int, int>();

    /// <summary>The date a fixed schedule without a maturity date stores, in ISO form.</summary>
    public string NoMaturityDateIso { get; init; } = "";

    /// <summary>The asset item types, in display order.</summary>
    public IReadOnlyList<string> AssetItemTypes { get; init; } = [];
}
