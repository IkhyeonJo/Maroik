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
        Deleted = account.Deleted
    };
}
