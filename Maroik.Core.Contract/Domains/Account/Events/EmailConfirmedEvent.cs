using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Account.Events;

/// <summary>
/// Raised when the user clicks the registration confirmation link and the token is validated successfully.
/// Consumers may send a "welcome" email or initialise default resources for the account.
/// </summary>
public sealed record EmailConfirmedEvent(Email Email) : DomainEvent;
