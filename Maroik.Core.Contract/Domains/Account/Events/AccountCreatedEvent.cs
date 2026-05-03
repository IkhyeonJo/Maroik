using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Account.Events;

/// <summary>
/// Raised when a new account is successfully created (before email confirmation).
/// Consumers may use this to send the registration confirmation email.
/// </summary>
public sealed record AccountCreatedEvent(Email Email) : DomainEvent;
