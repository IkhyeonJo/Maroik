using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IProfileService
{
    Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath);
    Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId);
    Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword);
}
