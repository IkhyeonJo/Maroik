using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Models.ViewModels.Managements;

namespace Maroik.Website.Mappers;

/// <summary>
/// Maps account response objects to Management-area display view models,
/// applying timezone conversion to audit timestamps.
/// </summary>
public static class ManagementViewModelMapper
{
    extension(AccountResponse item)
    {
        /// <summary>Maps a single <see cref="AccountResponse"/> to an <see cref="AccountOutputViewModel"/> for display.</summary>
        private AccountOutputViewModel ToDisplayViewModel(IPolicyService policy, string timeZoneIanaId) => new()
        {
            Email = item.Email,
            Nickname = item.Nickname,
            AvatarImagePath = item.AvatarImagePath,
            Role = item.Role,
            TimeZoneIanaId = item.TimeZoneIanaId,
            Locked = item.Locked,
            LoginAttempt = item.LoginAttempt,
            EmailConfirmed = item.EmailConfirmed,
            AgreedServiceTerms = item.AgreedServiceTerms,
            Created = policy.ToTimeZone(item.Created, timeZoneIanaId),
            Updated = policy.ToTimeZone(item.Updated, timeZoneIanaId),
            Message = item.Message,
            Deleted = item.Deleted,
            FileManagerScanDisabled = item.FileManagerScanDisabled
        };
    }

    extension(IEnumerable<AccountResponse> items)
    {
        /// <summary>Maps all <see cref="AccountResponse"/> items to display view models.</summary>
        public List<AccountOutputViewModel> ToDisplayViewModels(IPolicyService policy, string timeZoneIanaId)
            =>
            [
                .. items.Select(i => i.ToDisplayViewModel(policy, timeZoneIanaId))
            ];
    }
}
