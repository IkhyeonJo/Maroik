using Maroik.Core.Contract.Messages;
using Maroik.Core.Domain.Finances;
using Maroik.FileStorage.Helpers;
using Maroik.FileStorage.Interfaces;
using Maroik.FileStorage.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Services;

/// <summary>
/// The class-suggestion model files on disk. A model is saved only at <c>upload/ClassModels/{guid}.zip</c>
/// (<see cref="ClassModel.IsStoragePath"/>), never over an existing file, and without a virus scan or a size limit: the worker
/// trained and wrote it, no user uploaded it, and this service is reachable only on the internal network. Only such a file can be
/// deleted (the worker deletes the model a retraining replaced). Every path goes through <see cref="StoragePath"/> and
/// <see cref="StorageFiles"/> like the rest of the service.
/// </summary>
public sealed class ClassModelStorageService(IOptions<FileStorageSetting> settings) : IClassModelStorageService
{
    /// <summary>The model folder and the file at <paramref name="filePath"/> when it is a model file's path, else null.</summary>
    private (StoragePath Directory, StoragePath File)? ModelFile(string? filePath) =>
        ClassModel.IsStoragePath(filePath)
        && StoragePath.ResolveWithinRoot(settings.Value.StorageRootPath, ClassModel.StorageDirectory) is { } directory
        && directory.InDirectory(Path.GetFileName(filePath)) is { } file
            ? (directory, file)
            : null;

    /// <inheritdoc />
    public async Task<string?> SaveAsync(string? filePath, Stream content, CancellationToken ct = default)
    {
        if (ModelFile(filePath) is not var (directory, file))
            return FileStorageRefusals.NotAClassModelFile;

        using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, ct);
        if (buffered.Length == 0)
            return FileStorageRefusals.InvalidUpload;

        StorageFiles.CreateDirectory(directory);
        if (StorageFiles.Exists(file))
            return FileStorageRefusals.FileExists;

        await using (var stream = StorageFiles.CreateNew(file))
        {
            buffered.Position = 0;
            await buffered.CopyToAsync(stream, ct);
        }

        StorageFiles.MakeReadable(file);
        return null;
    }

    /// <inheritdoc />
    public string? Delete(string? filePath)
    {
        if (ModelFile(filePath) is not var (_, file))
            return FileStorageRefusals.NotAClassModelFile;
        if (!StorageFiles.Exists(file))
            return FileStorageRefusals.FileNotFound;

        StorageFiles.Delete(file);
        return null;
    }
}
