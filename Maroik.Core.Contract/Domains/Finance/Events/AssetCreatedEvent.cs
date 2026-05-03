using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance.Events;

/// <summary>
/// Raised when a new financial asset (bank account, stock account, cash, etc.) is registered.
/// </summary>
public sealed record AssetCreatedEvent(
    string ProductName,
    Email AccountEmail,
    Money InitialBalance) : DomainEvent;
