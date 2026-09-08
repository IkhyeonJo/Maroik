using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IProfileService"/> for user self-service profile management.
/// Avatar images are image-validated locally, then stored through the file-storage service
/// (<see cref="IFileClient"/> — shared across replicas, virus-scanned there). Password changes
/// require the current password to be verified via BCrypt before the new hash is stored.
/// Time-zone updates validate the IANA ID against the system's zone database.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IPasswordService _passwordService;
    private readonly IFileClient _fileClient;
    private readonly IImageValidatorService _imageValidator;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<ProfileService> _logger;

    /// <summary>Relative storage directory (under the file-storage root) that holds uploaded avatars.</summary>
    private const string AvatarStorageDirectory = "upload/Management/Profile/Avatar";

    /// <summary>Initializes a new instance of <see cref="ProfileService"/> with the supplied dependencies.</summary>
    public ProfileService(
        IAccountRepository accountRepository,
        IAssetRepository assetRepository,
        IPasswordService passwordService,
        IFileClient fileClient,
        IImageValidatorService imageValidator,
        IOptions<ServerSetting> settings,
        ILogger<ProfileService> logger)
    {
        _accountRepository = accountRepository;
        _assetRepository = assetRepository;
        _passwordService = passwordService;
        _fileClient = fileClient;
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
                return ServiceResult.NotFound("Account.NotFound", "Input is invalid");

            account.UpdateProfile(avatarPath, account.TimeZone, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update avatar for {Email}", email);
            return ServiceResult.Failure("Profile.UpdateAvatarFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.NotFound("Account.NotFound", "Input is invalid");

            var tzResult = TimeZoneId.Create(timeZoneIanaId);
            if (tzResult.IsError)
                return ServiceResult.FromError(tzResult.FirstError);

            account.UpdateProfile(account.AvatarImagePath, tzResult.Value, account.DefaultMonetaryUnit);

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update timezone for {Email}", email);
            return ServiceResult.Failure("Profile.UpdateTimezoneFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.NotFound("Account.NotFound", "Input is invalid");

            if (!_passwordService.VerifyPassword(currentPassword, account.HashedPassword))
                return ServiceResult.Validation("Profile.WrongPassword", "Invalid password. Please check again.");

            // Enforce the complexity rule server-side, before hashing — Account.ChangePassword only sees the hash.
            if (!PasswordPolicy.IsValid(newPassword))
                return ServiceResult.Validation("Account.PasswordPolicy", PasswordPolicy.ViolationMessage);

            account.ChangePassword(_passwordService.HashPassword(newPassword));

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update password for {Email}", email);
            return ServiceResult.Failure("Profile.UpdatePasswordFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, CancellationToken ct = default)
    {
        try
        {
            if (!_imageValidator.IsValidImage(imageBytes))
                return ServiceResult.Validation("Profile.InvalidImage", "invalid-image");
            if (_imageValidator.IsSvg(imageBytes))
                return ServiceResult.Validation("Profile.SvgNotAllowed", "svg-not-allowed");

            string avatarFile = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            // Store through the file-storage service (shared volume, ClamAV-scanned there) instead of
            // this replica's local wwwroot, so a multi-replica deployment stays consistent.
            bool uploaded = await _fileClient.UploadAsync(
                imageBytes, ContentTypeForExtension(extension),
                $"{AvatarStorageDirectory}/{avatarFile}", _settings.Value.FileStorageBaseUrl ?? "", ct);

            if (!uploaded)
                return ServiceResult.Failure("Profile.UploadAvatarFailed", "Input is invalid");

            return await UpdateAvatarAsync(email, $"/{AvatarStorageDirectory}/{avatarFile}", ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to upload and update avatar for {Email}", email);
            return ServiceResult.Failure("Profile.UploadAvatarFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadAvatarAsync(string fileName, CancellationToken ct = default)
    {
        try
        {
            byte[] bytes = await _fileClient.DownloadAsync(
                $"{AvatarStorageDirectory}/{fileName}", _settings.Value.FileStorageBaseUrl ?? "", ct);
            return bytes.Length > 0 ? bytes : null;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to download avatar {FileName} from file storage", fileName);
            return null;
        }
    }

    /// <summary>Maps an avatar file extension to its image MIME type.</summary>
    private static string ContentTypeForExtension(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

    private Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _accountRepository.FindByEmailAsync(email, ct);

    /// <inheritdoc />
    public async Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit, CancellationToken ct = default)
    {
        List<Asset> assets = await _assetRepository.GetByAccountEmailAsync(accountEmail, ct);
        Account? account = await FindByEmailAsync(accountEmail, ct);
        if (account == null) return;

        // A deleted asset's currency must not remain selectable as the default monetary unit.
        string? newUnit = assets.Where(x => !x.Deleted).Select(x => x.Balance.Currency).Distinct().Contains(monetaryUnit)
            ? monetaryUnit
            : null;
        account.UpdateProfile(account.AvatarImagePath, account.TimeZone, newUnit);
        await _accountRepository.UpdateEntityAsync(account, ct);
    }
}
