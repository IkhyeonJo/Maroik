namespace Maroik.FileStorage.Interfaces;

/// <summary>
/// The class-suggestion model files on disk (<c>upload/ClassModels/{guid}.zip</c>): the worker saves a newly trained model under a new
/// GUID and deletes the one it replaced. A refusal is returned as one of the <c>FileStorageRefusals</c> texts.
/// </summary>
public interface IClassModelStorageService
{
    /// <summary>Saves <paramref name="content"/> at <paramref name="filePath"/>; returns null, or the refusal.</summary>
    Task<string?> SaveAsync(string? filePath, Stream content, CancellationToken ct = default);

    /// <summary>Deletes the model file at <paramref name="filePath"/>; returns null, or the refusal.</summary>
    string? Delete(string? filePath);
}
