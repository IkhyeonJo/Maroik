using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Account.Events;

/// <summary>
/// Raised when an account is locked (e.g. too many failed login attempts or manual admin lock).
/// Consumers may notify the account owner by email.
/// </summary>
public sealed record AccountLockedEvent(Email Email) : DomainEvent;
