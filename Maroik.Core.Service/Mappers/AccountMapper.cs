using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

/// <summary>Maps <see cref="Account"/> domain objects to <see cref="AccountResponse"/> DTOs.</summary>
internal static class AccountMapper
{
    /// <summary>Converts an <see cref="Account"/> domain object to its corresponding <see cref="AccountResponse"/> DTO.</summary>
    internal static AccountResponse ToResponse(Account account) => new()
    {
        Email = account.Email.Value,
        Nickname = account.Nickname,
        AvatarImagePath = account.AvatarImagePath,
        Role = account.Role,
        TimeZoneIanaId = account.TimeZone.Value,
        DefaultMonetaryUnit = account.DefaultMonetaryUnit,
        Locked = account.Locked,
        LoginAttempt = account.LoginAttempt,
        EmailConfirmed = account.EmailConfirmed,
        AgreedServiceTerms = account.AgreedServiceTerms,
        Created = account.Created,
        Updated = account.Updated,
        Message = account.Message,
        Deleted = account.Deleted,
        SecurityStamp = account.SecurityStamp,
        MustChangePassword = account.MustChangePassword
    };

    /// <summary>
    /// Same as <see cref="ToResponse"/> but additionally populates <see cref="AccountResponse.HashedPassword"/>,
    /// <see cref="AccountResponse.RegistrationToken"/>, and <see cref="AccountResponse.ResetPasswordToken"/>.
    /// Use ONLY for the admin account-management grid/search/export (matching main's behavior there) —
    /// never for the logged-in user's own session-stored <see cref="AccountResponse"/>, since that would
    /// put the password hash into the session payload.
    /// </summary>
    internal static AccountResponse ToAdminResponse(Account account)
    {
        AccountResponse response = ToResponse(account);
        response.HashedPassword = account.HashedPassword;
        response.RegistrationToken = account.RegistrationToken;
        response.ResetPasswordToken = account.ResetPasswordToken;
        return response;
    }
}
