using System.Globalization;
using Maroik.Website.Models.ViewModels.Management;

namespace Maroik.Website.Extensions;

/// <summary>
/// Whole-text search filtering for the Management-area account grid view model.
/// </summary>
public static class ManagementSearchExtensions
{
    extension(IEnumerable<AccountOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those containing <paramref name="search"/> in any display field.
        /// Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<AccountOutputViewModel> ApplySearch(string? search)
        {
            if (string.IsNullOrEmpty(search)) return items;
            return items.Where(i =>
                Has(i.Email, search) ||
                Has(i.HashedPassword, search) ||
                Has(i.Nickname, search) ||
                Has(i.AvatarImagePath, search) ||
                Has(i.Role, search) ||
                Has(i.TimeZoneIanaId, search) ||
                Has(i.Locked.ToString(), search) ||
                Has(i.LoginAttempt.ToString(), search) ||
                Has(i.EmailConfirmed.ToString(), search) ||
                Has(i.AgreedServiceTerms.ToString(), search) ||
                Has(i.RegistrationToken, search) ||
                Has(i.ResetPasswordToken, search) ||
                Has(i.Created.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Updated.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Message, search) ||
                Has(i.Deleted.ToString(), search));
        }
    }

    private static bool Has(string? value, string search) => (value ?? "").Contains(search);
}
