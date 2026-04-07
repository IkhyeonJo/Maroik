using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for user profile self-management (Management > Profile page).
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the profile of the given account, or null if not found.</summary>
    Task<AccountResponse?> GetProfileAsync(string email);

    /// <summary>Sets the avatar image path to an existing server-side path (e.g. a preset avatar).</summary>
    Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath);

    /// <summary>
    /// Saves uploaded image bytes to <paramref name="avatarDirectoryPath"/> and updates the account's avatar path.
    /// </summary>
    Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, string avatarDirectoryPath);

    /// <summary>Updates the account's preferred IANA time-zone ID.</summary>
    Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId);

    /// <summary>
    /// Changes the account's password after verifying <paramref name="currentPassword"/> matches the stored hash.
    /// </summary>
    Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword);
}
