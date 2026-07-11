using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for user profile self-management (Management > Profile page).
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the profile of the given account, or null if not found.</summary>
    Task<AccountResponse?> GetProfileAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Saves uploaded image bytes to <paramref name="avatarDirectoryPath"/> and updates the account's avatar path.
    /// </summary>
    Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, string avatarDirectoryPath, CancellationToken ct = default);

    /// <summary>Updates the account's preferred IANA time-zone ID.</summary>
    Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId, CancellationToken ct = default);

    /// <summary>
    /// Changes the account's password after verifying <paramref name="currentPassword"/> matches the stored hash.
    /// </summary>
    Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Persists the user's preferred default currency unit.
    /// Sets the unit only when it matches one of the account's existing asset currencies;
    /// otherwise clears the preference to null.
    /// </summary>
    Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit, CancellationToken ct = default);
}
