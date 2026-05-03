using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance.Events;

/// <summary>
/// Raised when a recurring fixed income (e.g. salary, pension) is registered.
/// </summary>
public sealed record FixedIncomeRegisteredEvent(
    long FixedIncomeId,
    Email AccountEmail,
    Money Amount,
    string Content) : DomainEvent;
