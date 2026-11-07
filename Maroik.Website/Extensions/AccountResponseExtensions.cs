using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Accounts;

namespace Maroik.Website.Extensions;

/// <summary>
/// Checked reads of the request's account (<c>HttpContext.GetLoggedInAccount()</c>) fields that are never null in practice:
/// the database stores Email, Nickname, TimeZoneIanaId and AvatarImagePath as NOT NULL, and the anonymous placeholder fills
/// all of them but the email. <see cref="AccountResponse"/> is a shared DTO whose fields are nullable, so the controllers
/// used to write <c>!</c> or a made-up fallback (<c>?? "UTC"</c>, <c>?? ""</c>) instead; these reads replace both and throw,
/// naming the field, if the impossible happens — rather than handing a null or a wrong default on to a service.
/// </summary>
public static class AccountResponseExtensions
{
    extension(AccountResponse account)
    {
        /// <summary>The email of a signed-in account. Throws for the anonymous placeholder, which has none.</summary>
        /// <exception cref="InvalidOperationException">The account has no email.</exception>
        public string RequireEmail() => Required(account.Email, nameof(AccountResponse.Email));

        /// <summary>The account's nickname.</summary>
        /// <exception cref="InvalidOperationException">The account has no nickname.</exception>
        public string RequireNickname() => Required(account.Nickname, nameof(AccountResponse.Nickname));

        /// <summary>The account's IANA time-zone id.</summary>
        /// <exception cref="InvalidOperationException">The account has no time zone.</exception>
        public string RequireTimeZoneIanaId() => Required(account.TimeZoneIanaId, nameof(AccountResponse.TimeZoneIanaId));

        /// <summary>The account's avatar image path.</summary>
        /// <exception cref="InvalidOperationException">The account has no avatar image path.</exception>
        public string RequireAvatarImagePath() => Required(account.AvatarImagePath, nameof(AccountResponse.AvatarImagePath));
    }

    extension(AccountResponse? current)
    {
        /// <summary>
        /// Why <paramref name="session"/> no longer stands for this account's current row (null when the account is gone), or
        /// <see langword="null"/> while it still does: <see cref="AccountSessionPolicy"/> over the two DTOs.
        /// </summary>
        /// <param name="session">The account the session captured at login.</param>
        public SessionInvalidationReason? SessionInvalidationReason(AccountResponse session) => AccountSessionPolicy.InvalidationReason(
            current != null, current?.Deleted ?? false, current?.SecurityStamp, session.SecurityStamp);
    }

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidOperationException($"The account of this request has no {field} (no email means the anonymous placeholder).");
}
