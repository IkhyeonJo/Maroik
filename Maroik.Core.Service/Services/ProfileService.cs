using ImageMagick;
using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Domains.ValueObjects;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IProfileService"/> for user self-service profile management.
/// Avatar images are scanned by ClamAV (<see cref="IClamavClient"/>) before being processed
/// with ImageMagick (resize/compress) and uploaded to the file-storage microservice.
/// Password changes require the current password to be verified via BCrypt before the new
/// hash is stored. Time-zone updates validate the IANA ID against the system's zone database.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly IClamavClient _clamavClient;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<ProfileService> _logger;

    /// <summary>Initializes a new instance of <see cref="ProfileService"/> with the supplied dependencies.</summary>
    public ProfileService(
        IAccountRepository accountRepository,
        IPasswordService passwordService,
        IClamavClient clamavClient,
        IOptions<ServerSetting> settings,
        ILogger<ProfileService> logger)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _clamavClient = clamavClient;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AccountResponse?> GetProfileAsync(string email)
    {
        Account? account = await _accountRepository.GetAccountByEmailAsync(email);
        return account == null ? null : AccountMapper.ToResponse(account);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath)
    {
        try
        {
            Account? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            account.UpdateProfile(account.Nickname, avatarPath, account.TimeZone, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateAccountAsync(account);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update avatar for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId)
    {
        try
        {
            Account? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            var tzResult = TimeZoneId.Create(timeZoneIanaId);
            if (tzResult.IsError)
                return ServiceResult.Fail(tzResult.FirstError.Description);

            account.UpdateProfile(account.Nickname, account.AvatarImagePath, tzResult.Value, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateAccountAsync(account);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update timezone for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword)
    {
        try
        {
            Account? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            if (!_passwordService.VerifyPassword(currentPassword, account.HashedPassword))
                return ServiceResult.Fail("Invalid password. Please check again.");

            account.ChangePassword(_passwordService.HashPassword(newPassword));

            await _accountRepository.UpdateAccountAsync(account);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update password for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, string avatarDirectoryPath)
    {
        using var memoryStream = new MemoryStream(imageBytes);

        if (!await _clamavClient.ScanWithClamavAsync(memoryStream, _settings.Value.ClamavHost ?? string.Empty, _settings.Value.ClamavPort))
            return ServiceResult.Fail("virus-detected");

        memoryStream.Position = 0;
        try
        {
            using var image = new MagickImage(memoryStream);
            if (image.Format == MagickFormat.Svg)
                return ServiceResult.Fail("svg-not-allowed");
        }
        catch
        {
            return ServiceResult.Fail("invalid-image");
        }

        if (!Directory.Exists(avatarDirectoryPath))
            Directory.CreateDirectory(avatarDirectoryPath);

        string guid = Guid.NewGuid().ToString().ToUpper();
        string avatarFile = $"{guid}{extension}";
        string filePath = Path.Combine(avatarDirectoryPath, avatarFile);

#pragma warning disable SCS0018
        await using (var stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        var chmod = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
#pragma warning disable SCS0001
            Arguments = $"644 \"{filePath}\"",
#pragma warning restore SCS0001
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(chmod))
        {
            await process?.WaitForExitAsync()!;
        }

        return await UpdateAvatarAsync(email, $"/upload/Management/Profile/Avatar/{avatarFile}");
    }
}
