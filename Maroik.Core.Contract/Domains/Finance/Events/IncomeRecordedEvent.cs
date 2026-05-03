using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance.Events;

/// <summary>
/// Raised when a one-time income entry is recorded.
/// Consumers may update asset balances or recalculate dashboard summaries.
/// </summary>
public sealed record IncomeRecordedEvent(
    long IncomeId,
    Email AccountEmail,
    Money Amount,
    string DepositAssetProductName) : DomainEvent;
