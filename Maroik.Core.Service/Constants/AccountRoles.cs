using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Service.Constants;

/// <summary>
/// The account roles, for the code outside the Service layer that may not read the Domain. An attribute argument such as
/// <c>[RequiredHttpPostAccess(Role = AccountRoles.Admin)]</c> must be a compile-time constant, so it cannot come from a
/// service call; each value here is the Domain's own constant, not a copy of its text.
/// </summary>
public static class AccountRoles
{
    /// <summary><see cref="Role.Admin"/>.</summary>
    public const string Admin = Role.Admin;

    /// <summary><see cref="Role.User"/>.</summary>
    public const string User = Role.User;

    /// <summary><see cref="Role.Anonymous"/>.</summary>
    public const string Anonymous = Role.Anonymous;
}
