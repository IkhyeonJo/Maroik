using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProfileService"/>.
/// All repository and dependency-service fakes are hand-written (Fixtures/).
/// Covers profile retrieval, nickname update, avatar update,
/// and password-change validation (current-password verification before update).
/// </summary>
public class ProfileServiceTests : IAsyncDisposable
{
    /// <summary>Fake <c>IAccountRepository</c> injected into the system under test.</summary>
    private readonly FakeAccountRepository _accountRepo = new();
    /// <summary>Fake <c>IAssetRepository</c> injected into the system under test.</summary>
    private readonly FakeAssetRepository _assetRepo = new();
    /// <summary>Fake <c>IPasswordService</c> injected into the system under test.</summary>
    private readonly FakePasswordService _passwordService = new();
    /// <summary>Fake <c>IFileClient</c> injected into the system under test.</summary>
    private readonly FakeFileClient _fileClient = new();
    /// <summary>Fake <c>IImageValidatorService</c> injected into the system under test.</summary>
    private readonly FakeImageValidatorService _imageValidator = EchoingImageValidator.Create();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<ProfileService> _logger = new();
    /// <summary>Settings with a local file-storage URL.</summary>
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://localhost:5001" });

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies.</summary>
    private ProfileService CreateSut() => new(
        _accountRepo,
        _assetRepo,
        _passwordService,
        _fileClient,
        _imageValidator,
        _settings,
        _unitOfWork,
        _logger,
        _time);

    // -- Helpers --------------------------------------------------------------

    /// <summary>A persisted asset named <paramref name="name"/> in <paramref name="unit"/>.</summary>
    private static Asset MakeAsset(string name, string unit = "KRW", bool deleted = false) =>
        Asset.Reconstitute(
            productName: name,
            accountEmail: "user@example.com",
            item: "Deposit",
            amount: 1000m,
            monetaryUnit: unit,
            note: null,
            deleted: deleted,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    /// <summary>A confirmed, unlocked account for <paramref name="email"/>.</summary>
    private static Account ActiveAccount(string email = "user@example.com", bool locked = false) =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "$2a$13$placeholder",
            nickname: "TestUser",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: "User",
            timeZoneIanaId: "UTC",
            defaultMonetaryUnit: null,
            locked: locked,
            loginAttempt: locked ? 3 : 0,
            emailConfirmed: true,
            agreedServiceTerms: true,
            registrationToken: null,
            resetPasswordToken: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            message: null,
            deleted: false,
            securityStamp: "stamp",
            mustChangePassword: false);

    // -- GetProfileAsync ------------------------------------------------------

    /// <summary>Verifies that <c>GetProfileAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetProfileAsync_DelegatesToRepository()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        AccountResponse? result = await sut.GetProfileAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(account.Email.Value, result.Email);
    }

    // -- UpdateAvatarAsync ----------------------------------------------------

    /// <summary>Verifies that <c>UpdateAvatarAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync("ghost@example.com", "/new/path.jpg", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAvatarAsync</c> updates avatar path when account found.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_UpdatesAvatarPath_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateAvatarPathAsync((_, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync(account.Email.Value, "/new/avatar.jpg", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateAvatarPathAsyncCalls, call => call.Email == account.Email.Value && call.AvatarImagePath == "/new/avatar.jpg");
    }

    // -- UploadAndUpdateAvatarAsync -----------------------------------------------

    /// <summary>Rejects a payload the image validator does not recognize, without touching storage.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsInvalidImage_WhenNotAnImage()
    {
        _imageValidator.AnswerIsValidImage(_ => false);
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync("user@example.com", [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("invalid-image", result.ErrorKey);
        Assert.Empty(_fileClient.UploadWithResultAsyncCalls);
    }

    /// <summary>Rejects an SVG even though it is a "valid image", without touching storage.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsSvgNotAllowed_WhenSvg()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => true);
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync("user@example.com", [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("svg-not-allowed", result.ErrorKey);
        Assert.Empty(_fileClient.UploadWithResultAsyncCalls);
    }

    /// <summary>A valid image is stored under the avatar directory with the right MIME type, and the account path is updated.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_StoresImageAndUpdatesAccountPath_WhenValid()
    {
        var account = ActiveAccount();
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => false);
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Stored));
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateAvatarPathAsync((_, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync(account.Email.Value, [0xFF, 0xD8, 0xFF], ".JPG", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_fileClient.UploadWithResultAsyncCalls, call => call.ContentType == "image/jpeg" && (call.FilePath.StartsWith("upload/Management/Profile/Avatar/", StringComparison.Ordinal) && call.FilePath.EndsWith(".jpg", StringComparison.Ordinal)));
        Assert.Single(_accountRepo.UpdateAvatarPathAsyncCalls, call => call.Email == account.Email.Value && (call.AvatarImagePath!.StartsWith("/upload/Management/Profile/Avatar/", StringComparison.Ordinal)));
    }

    /// <summary>
    /// The avatar is stored as the validator's metadata-free re-encoding, never as the uploaded bytes, so a
    /// phone photo's EXIF (GPS position, camera, capture time) is not published to other users.
    /// </summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_StoresTheMetadataFreeImage_NotTheUploadedBytes()
    {
        var account = ActiveAccount();
        byte[] uploadedBytes = [0xFF, 0xD8, 0xFF, 0xE1, 0x45, 0x78, 0x69, 0x66];
        byte[] strippedBytes = [0xFF, 0xD8, 0xFF, 0xDB];
        _imageValidator.AnswerIsValidImage(bytesArg => bytesArg == uploadedBytes, _ => true);
        _imageValidator.AnswerIsSvg(bytesArg => bytesArg == uploadedBytes, _ => false);
        _imageValidator.AnswerStripMetadata(bytesArg => bytesArg == uploadedBytes, _ => new StrippedImage(strippedBytes, ".jpg", "image/jpeg"));
        byte[]? stored = null;
        _fileClient.AnswerUploadWithResultAsync((bytes, _, _, _, _) => { stored = bytes; return Task.FromResult<FileUploadResult>(FileUploadResult.Stored); });
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateAvatarPathAsync((_, _, _, _) => Task.FromResult<int>(1));

        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync(account.Email.Value, uploadedBytes, ".jpg", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(strippedBytes, stored);
    }

    /// <summary>A refused image is never re-encoded: the decoder only sees bytes the validator accepted.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_DoesNotReencode_AnImageThatFailedValidation()
    {
        _imageValidator.AnswerIsValidImage(_ => false);

        await CreateSut().UploadAndUpdateAvatarAsync("user@example.com", [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.Empty(_imageValidator.StripMetadataCalls);
    }

    /// <summary>A storage upload failure returns a failure result and never updates the account.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsFailure_WhenStorageUploadFails()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => false);
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Failed));
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync("user@example.com", [0xFF, 0xD8, 0xFF], ".png", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Profile.UploadAvatarFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.Empty(_accountRepo.UpdateAvatarPathAsyncCalls);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>Regression: a virus detection is reported as such (not as a generic "invalid input") and never updates the account.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsVirusDetected_WhenStorageFlagsTheFile()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => false);
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Infected));
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync("user@example.com", [0xFF, 0xD8, 0xFF], ".png", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("virus-detected", result.ErrorKey);
        Assert.Empty(_accountRepo.UpdateAvatarPathAsyncCalls);
    }

    /// <summary>A scanner outage is reported separately from a detection and never updates the account.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsScanUnavailable_WhenTheScannerCouldNotRun()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => false);
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.ScanUnavailable));
        var sut = CreateSut();

        ServiceResult result = await sut.UploadAndUpdateAvatarAsync("user@example.com", [0xFF, 0xD8, 0xFF], ".png", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("scan-unavailable", result.ErrorKey);
        Assert.Empty(_accountRepo.UpdateAvatarPathAsyncCalls);
    }

    // -- DownloadAvatarAsync ----------------------------------------------------

    /// <summary>Returns the bytes file storage hands back for a known avatar.</summary>
    [Fact]
    public async Task DownloadAvatarAsync_ReturnsBytes_WhenStorageHasFile()
    {
        byte[] payload = [1, 2, 3, 4];
        _fileClient.AnswerDownloadAsync((filePath, _, _) => filePath == "upload/Management/Profile/Avatar/abc.png", (_, _, _) => Task.FromResult<byte[]>(payload));
        var sut = CreateSut();

        byte[]? result = await sut.DownloadAvatarAsync("abc.png", TestContext.Current.CancellationToken);

        Assert.Equal(payload, result);
    }

    /// <summary>Returns null (not an exception) when file storage is unreachable / errors.</summary>
    [Fact]
    public async Task DownloadAvatarAsync_ReturnsNull_WhenStorageThrows()
    {
        _fileClient.AnswerDownloadAsync((_, _, _) => Task.FromException<byte[]>(new HttpRequestException("connection refused")));
        var sut = CreateSut();

        byte[]? result = await sut.DownloadAvatarAsync("abc.png", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Treats an empty response as "not found".</summary>
    [Fact]
    public async Task DownloadAvatarAsync_ReturnsNull_WhenStorageReturnsEmpty()
    {
        _fileClient.AnswerDownloadAsync((_, _, _) => Task.FromResult<byte[]>([]));
        var sut = CreateSut();

        byte[]? result = await sut.DownloadAvatarAsync("abc.png", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- UpdateTimeZoneAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdateTimeZoneAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync("ghost@example.com", "Asia/Seoul", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateTimeZoneAsync</c> updates time zone when account found.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_UpdatesTimeZone_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateTimeZoneAsync((_, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync(account.Email.Value, "Asia/Seoul", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateTimeZoneAsyncCalls, call => call.Email == account.Email.Value && call.TimeZoneIanaId == "Asia/Seoul");
    }

    // -- UpdatePasswordAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync("ghost@example.com", "old", "new", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> returns fail when current password is wrong.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenCurrentPasswordIsWrong()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "wrongpass" && hashedPassword == account.HashedPassword, (_, _) => false);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email.Value, "wrongpass", "newpass", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Invalid password", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> hashes and updates when current password is correct.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_HashesAndUpdates_WhenCurrentPasswordIsCorrect()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "oldpass" && hashedPassword == account.HashedPassword, (_, _) => true);
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email.Value, "oldpass", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdatePasswordAsyncCalls, call => call.Email == account.Email.Value && call.HashedPassword == "$2a$13$newhash" && call.MustChangePassword == false && call.ResetPasswordToken == null);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>
    /// A self-service password change must also persist the discarded reset token: this path writes
    /// column-scoped, so without passing the token column a reset link mailed earlier would stay
    /// valid in the database even though the domain object had cleared it.
    /// </summary>
    [Fact]
    public async Task UpdatePasswordAsync_PersistsClearedResetPasswordToken()
    {
        var account = ActiveAccount();
        Assert.False(account.RequestPasswordReset(GuidToken.Generate(Now), Now).IsError);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "oldpass" && hashedPassword == account.HashedPassword, (_, _) => true);
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email.Value, "oldpass", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdatePasswordAsyncCalls, call => call.Email == account.Email.Value && call.HashedPassword == "$2a$13$newhash" && call.MustChangePassword == false && call.ResetPasswordToken == null);
    }

    /// <summary>
    /// A signed-in owner whose account was locked by someone else's failed logins changes the password: the lock,
    /// the counter and the lock message are written off with it, so the sign-in the change asks for is not refused.
    /// </summary>
    [Fact]
    public async Task UpdatePasswordAsync_PersistsTheLiftedLock_WhenTheAccountWasLocked()
    {
        var account = ActiveAccount(locked: true);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "oldpass" && hashedPassword == account.HashedPassword, (_, _) => true);
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => Task.FromResult<int>(1));

        ServiceResult result = await CreateSut().UpdatePasswordAsync(account.Email.Value, "oldpass", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdatePasswordAsyncCalls, call => call.Email == account.Email.Value && call.HashedPassword == "$2a$13$newhash" && call.MustChangePassword == false && call.ResetPasswordToken == null && call.Locked == false && call.LoginAttempt == 0L && call.Message == null);
    }

    /// <summary>
    /// Regression test for the read-verify-write race this method used to have: the account lookup
    /// must be the locked (FOR UPDATE) variant, not the plain unlocked lookup, so a concurrent
    /// password reset on the same row cannot be silently overwritten by a stale verification.
    /// </summary>
    [Fact]
    public async Task UpdatePasswordAsync_UsesLockedLookup_NotUnlockedFindByEmail()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((password, hashedPassword) => password == "oldpass" && hashedPassword == account.HashedPassword, (_, _) => true);
        _passwordService.AnswerHashPassword(password => password == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        await sut.UpdatePasswordAsync(account.Email.Value, "oldpass", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.FindByEmailForUpdateAsyncCalls, call => call.Email == account.Email.Value);
        Assert.Empty(_accountRepo.FindByEmailAsyncCalls);
    }

    // -- UpdateDefaultMonetaryUnitAsync --------------------------------------

    /// <summary>A non-existent account resolves to a no-op column update (0 rows) and does not throw.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_IssuesColumnUpdate_EvenWhenAccountMissing()
    {
        _assetRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Asset>>([]));
        _accountRepo.AnswerUpdateDefaultMonetaryUnitAsync((_, _, _) => Task.FromResult<int>(0));
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync("missing@example.com", "KRW", TestContext.Current.CancellationToken);

        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Single(_accountRepo.UpdateDefaultMonetaryUnitAsyncCalls, call => call.Email == "missing@example.com" && call.DefaultMonetaryUnit == null);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> persists unit when it exists in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsUnit_WhenUnitExistsInAssets()
    {
        var account = ActiveAccount();
        _assetRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Asset>>([MakeAsset("Wallet")]));
        _accountRepo.AnswerUpdateDefaultMonetaryUnitAsync((_, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(account.Email.Value, "KRW", TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateDefaultMonetaryUnitAsyncCalls, call => call.Email == account.Email.Value && call.DefaultMonetaryUnit == "KRW");
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> clears unit when it is not in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsNull_WhenUnitNotInAssets()
    {
        var account = ActiveAccount();
        _assetRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Asset>>([MakeAsset("Wallet", "USD")]));
        _accountRepo.AnswerUpdateDefaultMonetaryUnitAsync((_, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(account.Email.Value, "KRW", TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateDefaultMonetaryUnitAsyncCalls, call => call.Email == account.Email.Value && call.DefaultMonetaryUnit == null);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> accepts a soft-deleted asset's currency (the dashboard counts deleted assets too).</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsUnit_WhenUnitOnlyExistsOnDeletedAsset()
    {
        var account = ActiveAccount();
        _assetRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Asset>>([MakeAsset("OldForeignAccount", "USD", deleted: true)]));
        _accountRepo.AnswerUpdateDefaultMonetaryUnitAsync((_, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(account.Email.Value, "USD", TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateDefaultMonetaryUnitAsyncCalls, call => call.Email == account.Email.Value && call.DefaultMonetaryUnit == "USD");
    }

    // -- Failure branches ---------------------------------------------------------

    /// <summary>A repository failure while saving the avatar path is caught and reported generically.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_ReportsUpdateAvatarFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdateAvatarAsync("user@example.com", "/a.png", TestContext.Current.CancellationToken);

        Assert.Equal("Profile.UpdateAvatarFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
    }

    /// <summary>An unknown IANA time zone is refused with the domain's validation error and nothing is written.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_ReturnsTheDomainValidationError_ForAnUnknownTimeZone()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));

        ServiceResult result = await CreateSut().UpdateTimeZoneAsync("user@example.com", "Mars/Olympus_Mons", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.NotEqual("Profile.UpdateTimezoneFailed", result.ErrorCode);
        Assert.Empty(_accountRepo.UpdateTimeZoneAsyncCalls);
    }

    /// <summary>A repository failure while saving the time zone is caught and reported generically.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_ReportsUpdateTimezoneFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdateTimeZoneAsync("user@example.com", "UTC", TestContext.Current.CancellationToken);

        Assert.Equal("Profile.UpdateTimezoneFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
    }

    /// <summary>A new password that breaks the complexity rule is refused before hashing and before any write; the lock is released.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsPolicyViolation_AndDoesNotHashOrWrite_WhenTheNewPasswordIsTooWeak()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        _passwordService.AnswerVerifyPassword((_, _) => true);

        ServiceResult result = await CreateSut().UpdatePasswordAsync("user@example.com", "current", "weak", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Account.PasswordPolicy", result.ErrorCode);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.UpdatePasswordAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A failure while persisting the new password is rolled back and reported generically.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_RollsBackAndReportsUpdatePasswordFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        _passwordService.AnswerHashPassword(_ => "$2a$13$new");
        _accountRepo.AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => Task.FromException<int>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdatePasswordAsync("user@example.com", "current", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal("Profile.UpdatePasswordFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>The extension allow-list is re-asserted in the service: a disallowed extension never reaches the validator or storage.</summary>
    [Theory]
    [InlineData(".gif")]
    [InlineData(".svg")]
    [InlineData("")]
    [InlineData(".png.exe")]
    public async Task UploadAndUpdateAvatarAsync_RefusesADisallowedExtension_BeforeDecodingOrStoring(string extension)
    {
        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync("user@example.com", [1, 2, 3], extension, TestContext.Current.CancellationToken);

        Assert.Equal("invalid-image", result.ErrorKey);
        Assert.Empty(_imageValidator.IsValidImageCalls);
        Assert.Empty(_fileClient.UploadWithResultAsyncCalls);
    }

    /// <summary>
    /// The avatar is stored under the format it actually is (by content), whatever allowed extension it was uploaded
    /// with: its storage name, its MIME type and the account's avatar path all carry that format's extension.
    /// </summary>
    [Theory]
    [InlineData(".jpg", false, ".jpg", "image/jpeg")]
    [InlineData(".JPEG", false, ".jpg", "image/jpeg")]
    [InlineData(".png", false, ".jpg", "image/jpeg")]
    [InlineData(".png", true, ".png", "image/png")]
    [InlineData(".PNG", true, ".png", "image/png")]
    [InlineData(".jpg", true, ".png", "image/png")]
    public async Task UploadAndUpdateAvatarAsync_StoresTheAvatarUnderTheFormatItActuallyIs(
        string uploadedExtension, bool pngContent, string storedExtension, string storedContentType)
    {
        byte[] bytes = pngContent ? [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00] : [0xFF, 0xD8, 0xFF, 0xDB];
        _imageValidator.AnswerIsValidImage(_ => true);
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Stored));
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        _accountRepo.AnswerUpdateAvatarPathAsync((_, _, _, _) => Task.FromResult<int>(1));

        await CreateSut().UploadAndUpdateAvatarAsync("user@example.com", bytes, uploadedExtension, TestContext.Current.CancellationToken);

        Assert.Single(_fileClient.UploadWithResultAsyncCalls, call => call.ContentType == storedContentType && (call.FilePath.EndsWith(storedExtension, StringComparison.Ordinal)));
        Assert.Single(_accountRepo.UpdateAvatarPathAsyncCalls, call => (call.AvatarImagePath!.EndsWith(storedExtension, StringComparison.Ordinal)));
    }

    /// <summary>An unexpected failure while validating/storing the avatar is caught and reported generically.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReportsUploadAvatarFailed_WhenTheValidatorThrows()
    {
        _imageValidator.AnswerIsSvg(_ => throw new InvalidOperationException("secret detail"));

        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync("user@example.com", [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.Equal("Profile.UploadAvatarFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
    }

    // -- Exact results, storage address and failure logging --------------------------------

    /// <summary>E-mail of the account these tests act on.</summary>
    private const string Email = "user@example.com";

    /// <summary>Asserts the one Error entry carries <paramref name="thrown"/> and reads <paramref name="message"/>.</summary>
    private void AssertLoggedError(string message, Exception thrown)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Same(thrown, record.Exception);
        Assert.Equal(message, record.Message);
    }

    /// <summary>Asserts <paramref name="result"/> is the account NotFound ("Input is invalid").</summary>
    private static void AssertAccountNotFound(ServiceResult result)
    {
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Account.NotFound", result.ErrorCode);
        Assert.Equal("Input is invalid", result.ErrorKey);
    }

    /// <summary>Makes the validator accept the upload as a (non-SVG) image.</summary>
    private void GivenAValidImage()
    {
        _imageValidator.AnswerIsValidImage(_ => true);
        _imageValidator.AnswerIsSvg(_ => false);
    }

    /// <summary>Makes the file storage answer every upload with <paramref name="outcome"/>.</summary>
    private void GivenTheStorageAnswers(FileUploadResult outcome) =>
        _fileClient.AnswerUploadWithResultAsync((_, _, _, _, _) => Task.FromResult<FileUploadResult>(outcome));

    /// <summary>An unknown e-mail has no profile.</summary>
    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenTheAccountDoesNotExist()
    {
        Assert.Null(await CreateSut().GetProfileAsync("ghost@example.com", TestContext.Current.CancellationToken));
    }

    /// <summary>The avatar of an unknown account cannot be changed: NotFound, and nothing is written.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_ReturnsAccountNotFound_WhenTheAccountIsUnknown()
    {
        AssertAccountNotFound(await CreateSut().UpdateAvatarAsync(Email, "/a.png", TestContext.Current.CancellationToken));
        Assert.Empty(_accountRepo.UpdateAvatarPathAsyncCalls);
    }

    /// <summary>An avatar update that throws is logged with the exception and the account.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == Email, (_, _) => Task.FromException<Account?>(thrown));

        await CreateSut().UpdateAvatarAsync(Email, "/a.png", TestContext.Current.CancellationToken);

        AssertLoggedError($"Failed to update avatar for {Email}", thrown);
    }

    /// <summary>The time zone of an unknown account cannot be changed: NotFound, and nothing is written.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_ReturnsAccountNotFound_WhenTheAccountIsUnknown()
    {
        AssertAccountNotFound(await CreateSut().UpdateTimeZoneAsync(Email, "Asia/Seoul", TestContext.Current.CancellationToken));
        Assert.Empty(_accountRepo.UpdateTimeZoneAsyncCalls);
    }

    /// <summary>A time-zone update that throws is logged with the exception and the account.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == Email, (_, _) => Task.FromException<Account?>(thrown));

        await CreateSut().UpdateTimeZoneAsync(Email, "Asia/Seoul", TestContext.Current.CancellationToken);

        AssertLoggedError($"Failed to update timezone for {Email}", thrown);
    }

    /// <summary>The password of an unknown account cannot be changed: NotFound, rolled back.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsAccountNotFound_AndRollsBack_WhenTheAccountIsUnknown()
    {
        AssertAccountNotFound(await CreateSut().UpdatePasswordAsync(Email, "Old1234!", "New1234!", TestContext.Current.CancellationToken));
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A wrong current password is the "invalid password" Validation error.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsWrongPassword_WithItsMessage_WhenTheCurrentPasswordIsWrong()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == Email, (_, _) => Task.FromResult<Account?>(ActiveAccount()));
        _passwordService.AnswerVerifyPassword((_, _) => false);

        ServiceResult result = await CreateSut().UpdatePasswordAsync(Email, "Wrong1234!", "New1234!", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Profile.WrongPassword", result.ErrorCode);
        Assert.Equal("Invalid password. Please check again.", result.ErrorKey);
    }

    /// <summary>A password change that throws is logged with the exception and the account (and never with a password).</summary>
    [Fact]
    public async Task UpdatePasswordAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == Email, (_, _) => Task.FromException<Account?>(thrown));

        await CreateSut().UpdatePasswordAsync(Email, "Old1234!", "New1234!", TestContext.Current.CancellationToken);

        AssertLoggedError($"Failed to update password for {Email}", thrown);
    }

    /// <summary>A disallowed extension is the "invalid-image" Validation error.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsInvalidImageValidation_ForADisallowedExtension()
    {
        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync(Email, [1, 2, 3], ".gif", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Profile.InvalidImage", result.ErrorCode);
    }

    /// <summary>An SVG is the "svg-not-allowed" Validation error.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsSvgNotAllowedValidation_ForAnSvg()
    {
        _imageValidator.AnswerIsSvg(_ => true);

        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync(Email, [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Profile.SvgNotAllowed", result.ErrorCode);
    }

    /// <summary>A payload that is not a JPEG/PNG is the "invalid-image" Validation error.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_ReturnsInvalidImageValidation_ForANonImage()
    {
        _imageValidator.AnswerIsValidImage(_ => false);

        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync(Email, [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Profile.InvalidImage", result.ErrorCode);
    }

    /// <summary>A virus detection is a Validation error (the user's file), a scanner outage a Failure (ours).</summary>
    [Theory]
    [InlineData(FileUploadResult.Infected, ServiceErrorType.Validation, "Profile.VirusDetected")]
    [InlineData(FileUploadResult.ScanUnavailable, ServiceErrorType.Failure, "Profile.ScanUnavailable")]
    public async Task UploadAndUpdateAvatarAsync_ClassifiesTheScanOutcome(FileUploadResult outcome, ServiceErrorType expectedType, string expectedCode)
    {
        GivenAValidImage();
        GivenTheStorageAnswers(outcome);

        ServiceResult result = await CreateSut().UploadAndUpdateAvatarAsync(Email, [0xFF, 0xD8, 0xFF], ".png", TestContext.Current.CancellationToken);

        Assert.Equal(expectedType, result.ErrorType);
        Assert.Equal(expectedCode, result.ErrorCode);
    }

    /// <summary>The avatar is uploaded to the configured file-storage address.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_UploadsToTheConfiguredFileStorage()
    {
        GivenAValidImage();
        GivenTheStorageAnswers(FileUploadResult.Failed);

        await CreateSut().UploadAndUpdateAvatarAsync(Email, [0xFF, 0xD8, 0xFF], ".png", TestContext.Current.CancellationToken);

        Assert.Single(_fileClient.UploadWithResultAsyncCalls, call => call.FileStorageBaseUrl == "http://localhost:5001");
    }

    /// <summary>An avatar upload that throws is logged with the exception and the account.</summary>
    [Fact]
    public async Task UploadAndUpdateAvatarAsync_LogsTheFailure_WhenTheValidatorThrows()
    {
        var thrown = new InvalidOperationException("boom");
        _imageValidator.AnswerIsSvg(_ => throw thrown);

        await CreateSut().UploadAndUpdateAvatarAsync(Email, [1, 2, 3], ".png", TestContext.Current.CancellationToken);

        AssertLoggedError($"Failed to upload and update avatar for {Email}", thrown);
    }

    /// <summary>An avatar is downloaded from the configured file-storage address.</summary>
    [Fact]
    public async Task DownloadAvatarAsync_DownloadsFromTheConfiguredFileStorage()
    {
        _fileClient.AnswerDownloadAsync((filePath, fileStorageBaseUrl, _) => filePath == "upload/Management/Profile/Avatar/abc.png" && fileStorageBaseUrl == "http://localhost:5001", (_, _, _) => Task.FromResult<byte[]>([1, 2, 3]));

        Assert.Equal([1, 2, 3], await CreateSut().DownloadAvatarAsync("abc.png", TestContext.Current.CancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
