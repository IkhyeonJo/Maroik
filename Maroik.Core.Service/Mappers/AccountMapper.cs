using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class AccountMapper
{
    internal static AccountResponse ToResponse(Account account) => new()
    {
        Email = account.Email.Value,
        HashedPassword = account.HashedPassword,
        Nickname = account.Nickname,
        AvatarImagePath = account.AvatarImagePath,
        Role = account.Role,
        TimeZoneIanaId = account.TimeZone.Value,
        DefaultMonetaryUnit = account.DefaultMonetaryUnit,
        Locked = account.Locked,
        LoginAttempt = account.LoginAttempt,
        EmailConfirmed = account.EmailConfirmed,
        AgreedServiceTerms = account.AgreedServiceTerms,
        RegistrationToken = account.RegistrationToken,
        ResetPasswordToken = account.ResetPasswordToken,
        Created = account.Created,
        Updated = account.Updated,
        Message = account.Message,
        Deleted = account.Deleted
    };
}
