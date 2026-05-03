using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance.Events;

/// <summary>
/// Raised when a one-time expenditure is recorded.
/// Consumers may update asset balances or recalculate dashboard summaries.
/// </summary>
public sealed record ExpenditureRecordedEvent(
    long ExpenditureId,
    Email AccountEmail,
    Money Amount,
    string PaymentMethod) : DomainEvent;
