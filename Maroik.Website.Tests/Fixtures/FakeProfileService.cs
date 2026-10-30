using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IProfileService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeProfileService : IProfileService
{
    /// <summary>What <c>GetProfileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<AccountResponse?>>? OnGetProfileAsync { get; set; }

    /// <summary>The arguments of every <c>GetProfileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> GetProfileAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<AccountResponse?> GetProfileAsync(string email, CancellationToken ct)
    {
        lock (GetProfileAsyncCalls) GetProfileAsyncCalls.Add((email, ct));
        if (OnGetProfileAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.FromResult<AccountResponse?>(default);
    }

    /// <summary>What <c>UploadAndUpdateAvatarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, byte[], string, CancellationToken, Task<ServiceResult>>? OnUploadAndUpdateAvatarAsync { get; set; }

    /// <summary>The arguments of every <c>UploadAndUpdateAvatarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, byte[] ImageBytes, string Extension, CancellationToken Ct)> UploadAndUpdateAvatarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UploadAndUpdateAvatarAsync(string email, byte[] imageBytes, string extension, CancellationToken ct)
    {
        lock (UploadAndUpdateAvatarAsyncCalls) UploadAndUpdateAvatarAsyncCalls.Add((email, imageBytes, extension, ct));
        if (OnUploadAndUpdateAvatarAsync is { } on)
        {
            return on(email, imageBytes, extension, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DownloadAvatarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<byte[]?>>? OnDownloadAvatarAsync { get; set; }

    /// <summary>The arguments of every <c>DownloadAvatarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string FileName, CancellationToken Ct)> DownloadAvatarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<byte[]?> DownloadAvatarAsync(string fileName, CancellationToken ct)
    {
        lock (DownloadAvatarAsyncCalls) DownloadAvatarAsyncCalls.Add((fileName, ct));
        if (OnDownloadAvatarAsync is { } on)
        {
            return on(fileName, ct);
        }

        return Task.FromResult<byte[]?>([]);
    }

    /// <summary>What <c>UpdateTimeZoneAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<ServiceResult>>? OnUpdateTimeZoneAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateTimeZoneAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string TimeZoneIanaId, CancellationToken Ct)> UpdateTimeZoneAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId, CancellationToken ct)
    {
        lock (UpdateTimeZoneAsyncCalls) UpdateTimeZoneAsyncCalls.Add((email, timeZoneIanaId, ct));
        if (OnUpdateTimeZoneAsync is { } on)
        {
            return on(email, timeZoneIanaId, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdatePasswordAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, CancellationToken, Task<ServiceResult>>? OnUpdatePasswordAsync { get; set; }

    /// <summary>The arguments of every <c>UpdatePasswordAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string CurrentPassword, string NewPassword, CancellationToken Ct)> UpdatePasswordAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct)
    {
        lock (UpdatePasswordAsyncCalls) UpdatePasswordAsyncCalls.Add((email, currentPassword, newPassword, ct));
        if (OnUpdatePasswordAsync is { } on)
        {
            return on(email, currentPassword, newPassword, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateDefaultMonetaryUnitAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, CancellationToken, Task>? OnUpdateDefaultMonetaryUnitAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateDefaultMonetaryUnitAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string? MonetaryUnit, CancellationToken Ct)> UpdateDefaultMonetaryUnitAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit, CancellationToken ct)
    {
        lock (UpdateDefaultMonetaryUnitAsyncCalls) UpdateDefaultMonetaryUnitAsyncCalls.Add((accountEmail, monetaryUnit, ct));
        if (OnUpdateDefaultMonetaryUnitAsync is { } on)
        {
            return on(accountEmail, monetaryUnit, ct);
        }

        return Task.CompletedTask;
    }
}
