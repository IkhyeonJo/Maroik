using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance.Events;

/// <summary>
/// Raised when a recurring fixed expenditure (e.g. subscription, insurance) is registered.
/// </summary>
public sealed record FixedExpenditureRegisteredEvent(
    long FixedExpenditureId,
    Email AccountEmail,
    Money Amount,
    string Content) : DomainEvent;
