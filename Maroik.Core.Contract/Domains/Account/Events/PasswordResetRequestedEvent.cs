using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Account.Events;

/// <summary>
/// Raised when a password-reset token is generated and must be dispatched to the account owner.
/// Consumers are expected to send the <paramref name="ResetToken"/> embedded in a reset-password email link.
/// </summary>
public sealed record PasswordResetRequestedEvent(Email Email, string ResetToken) : DomainEvent;
