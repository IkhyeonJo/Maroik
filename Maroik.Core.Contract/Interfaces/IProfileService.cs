using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IProfileService
{
    Task<AccountResponse?> GetProfileAsync(string email);
    Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath);
    Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, string avatarDirectoryPath);
    Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId);
    Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword);
}
