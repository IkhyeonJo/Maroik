using System.Diagnostics;
using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IProfileService"/> for user self-service profile management.
/// Avatar images are scanned by ClamAV (<see cref="IClamavClient"/>) and image-validated
/// before being stored. Password changes require the current password to be verified via BCrypt
/// before the new hash is stored. Time-zone updates validate the IANA ID against the system's zone database.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IPasswordService _passwordService;
    private readonly IClamavClient _clamavClient;
    private readonly IImageValidatorService _imageValidator;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<ProfileService> _logger;

    /// <summary>Initializes a new instance of <see cref="ProfileService"/> with the supplied dependencies.</summary>
    public ProfileService(
        IAccountRepository accountRepository,
        IAssetRepository assetRepository,
        IPasswordService passwordService,
        IClamavClient clamavClient,
        IImageValidatorService imageValidator,
        IOptions<ServerSetting> settings,
        ILogger<ProfileService> logger)
    {
        _accountRepository = accountRepository;
        _assetRepository = assetRepository;
        _passwordService = passwordService;
        _clamavClient = clamavClient;
        _imageValidator = imageValidator;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AccountResponse?> GetProfileAsync(string email, CancellationToken ct = default)
    {
        Account? account = await FindByEmailAsync(email, ct);
        return account == null ? null : AccountMapper.ToResponse(account);
    }

    /// <inheritdoc cref="avatarPath" />
    public async Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            account.UpdateProfile(account.Nickname, avatarPath, account.TimeZone, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update avatar for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            var tzResult = TimeZoneId.Create(timeZoneIanaId);
            if (tzResult.IsError)
                return ServiceResult.Fail(tzResult.FirstError.Description);

            account.UpdateProfile(account.Nickname, account.AvatarImagePath, tzResult.Value, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update timezone for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            if (!_passwordService.VerifyPassword(currentPassword, account.HashedPassword))
                return ServiceResult.Fail("Invalid password. Please check again.");

            account.ChangePassword(_passwordService.HashPassword(newPassword));

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update password for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, string avatarDirectoryPath, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream(imageBytes);

        if (!await _clamavClient.ScanWithClamavAsync(memoryStream, _settings.Value.ClamavHost ?? "", _settings.Value.ClamavPort, ct))
            return ServiceResult.Fail("virus-detected");

        memoryStream.Position = 0;
        if (!_imageValidator.IsValidImage(memoryStream.ToArray()))
            return ServiceResult.Fail("invalid-image");
        if (_imageValidator.IsSvg(memoryStream.ToArray()))
            return ServiceResult.Fail("svg-not-allowed");

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
            await memoryStream.CopyToAsync(stream, ct);
            await stream.FlushAsync(ct);
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
            await process?.WaitForExitAsync(ct)!;
        }

        return await UpdateAvatarAsync(email, $"/upload/Management/Profile/Avatar/{avatarFile}", ct);
    }

    private Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _accountRepository.FindByEmailAsync(email, ct);

    /// <inheritdoc />
    public async Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit, CancellationToken ct = default)
    {
        List<Asset> assets = await _assetRepository.GetByAccountEmailAsync(accountEmail, ct);
        Account? account = await FindByEmailAsync(accountEmail, ct);
        if (account == null) return;

        string? newUnit = assets.Select(x => x.Balance.Currency).Distinct().Contains(monetaryUnit)
            ? monetaryUnit
            : null;
        account.UpdateProfile(account.Nickname, account.AvatarImagePath, account.TimeZone, newUnit);
        await _accountRepository.UpdateEntityAsync(account, ct);
    }
}
