using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ClassModelStorageService"/> against a real throwaway storage root: a class-suggestion model file is saved
/// only at <c>upload/ClassModels/{guid}.zip</c> and never over an existing file, with no virus scan (the worker wrote it, no user did),
/// and only such a file can be deleted.
/// </summary>
public sealed class ClassModelStorageServiceTests : IDisposable
{
    /// <summary>The throwaway parent of the storage root.</summary>
    private readonly string _base = Path.Combine(Path.GetTempPath(), "cm-" + Guid.NewGuid().ToString("N"));

    /// <summary>The storage root (<c>{base}/upload</c>), as the service is configured with it.</summary>
    private string Root => Path.Combine(_base, "upload");

    /// <summary>The model path these tests use.</summary>
    private const string ModelPath = "upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip";

    /// <summary>Where <see cref="ModelPath"/> lands on disk.</summary>
    private string ModelFile => Path.Combine(Root, "ClassModels", "0f8fad5b-d9cb-469f-a165-70867728950e.zip");

    /// <summary>Creates the storage root.</summary>
    public ClassModelStorageServiceTests() => Directory.CreateDirectory(Root);

    /// <summary>Removes the throwaway storage.</summary>
    public void Dispose() => Directory.Delete(_base, recursive: true);

    /// <summary>The service under test, over the throwaway root.</summary>
    private ClassModelStorageService Sut => new(Options.Create(new FileStorageSetting { StorageRootPath = Root }));

    /// <summary>Saves <paramref name="bytes"/> at <paramref name="path"/>.</summary>
    private async Task<string?> SaveAsync(string? path, byte[] bytes)
    {
        using var content = new MemoryStream(bytes);
        return await Sut.SaveAsync(path, content, TestContext.Current.CancellationToken);
    }

    /// <summary>A model file is written at its path, creating the folder.</summary>
    [Fact]
    public async Task SaveAsync_WritesTheFile()
    {
        Assert.Null(await SaveAsync(ModelPath, [1, 2, 3]));

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(ModelFile, TestContext.Current.CancellationToken));
    }

    /// <summary>An existing file is never overwritten: a retraining writes a new GUID.</summary>
    [Fact]
    public async Task SaveAsync_NeverOverwrites()
    {
        await SaveAsync(ModelPath, [1]);

        Assert.Equal(FileStorageRefusals.FileExists, await SaveAsync(ModelPath, [2]));
        Assert.Equal([1], await File.ReadAllBytesAsync(ModelFile, TestContext.Current.CancellationToken));
    }

    /// <summary>Any other path is refused before the disk is touched: an attachment folder, a traversal, a missing extension.</summary>
    [Theory]
    [InlineData("upload/Forum/0f8fad5b-d9cb-469f-a165-70867728950e.zip")]
    [InlineData("upload/ClassModels/../Forum/0f8fad5b-d9cb-469f-a165-70867728950e.zip")]
    [InlineData("upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData(null)]
    public async Task SaveAsync_RefusesAnyOtherPath(string? path)
    {
        Assert.Equal(FileStorageRefusals.NotAClassModelFile, await SaveAsync(path, [1]));
        Assert.False(Directory.Exists(Path.Combine(Root, "ClassModels")));
    }

    /// <summary>An empty model is no model.</summary>
    [Fact]
    public async Task SaveAsync_RefusesAnEmptyFile()
    {
        Assert.Equal(FileStorageRefusals.InvalidUpload, await SaveAsync(ModelPath, []));
        Assert.False(File.Exists(ModelFile));
    }

    /// <summary>A model file is deleted; deleting it again says it is not there; another path is refused and left alone.</summary>
    [Fact]
    public async Task Delete_RemovesOnlyAModelFile()
    {
        await SaveAsync(ModelPath, [1]);
        string attachment = Path.Combine(Root, "Forum", "a.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(attachment)!);
        await File.WriteAllBytesAsync(attachment, [1], TestContext.Current.CancellationToken);

        Assert.Null(Sut.Delete(ModelPath));
        Assert.False(File.Exists(ModelFile));
        Assert.Equal(FileStorageRefusals.FileNotFound, Sut.Delete(ModelPath));
        Assert.Equal(FileStorageRefusals.NotAClassModelFile, Sut.Delete("upload/Forum/a.zip"));
        Assert.True(File.Exists(attachment));
    }
}
