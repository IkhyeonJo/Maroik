using Maroik.Core.Domain.FileManagers;
namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>
/// Unit tests for <see cref="FileManagerNode"/>: a folder or a file of a user's drive. Folders are logical (database only); a file
/// is stored under a GUID name in <c>upload/FileManager</c> and is "uploading" until its upload completes. Names follow
/// <see cref="FileManagerNamePolicy"/>; a folder cannot be moved into itself or its own sub-folder.
/// </summary>
public class FileManagerNodeTests
{
    /// <summary>The fixed "current time" every domain call in this class receives.</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A storage id for the uploads these tests start.</summary>
    private static readonly Guid _storageId = Guid.Parse("8f1e9c0a-1b2c-4d5e-8f70-112233445566");

    /// <summary>A stored folder with the given id and parent.</summary>
    private static FileManagerNode Folder(long id, long? parentId = null, string name = "Design") =>
        FileManagerNode.Reconstitute(id, "user@example.com", parentId, name, isFolder: true, 0, null, uploading: false, _now, _now);

    /// <summary>A stored, completed file with the given id.</summary>
    private static FileManagerNode File(long id, long? parentId = null) =>
        FileManagerNode.Reconstitute(id, "user@example.com", parentId, "a.txt", isFolder: false, 10, "upload/FileManager/x", uploading: false, _now, _now);

    // -- CreateFolder -------------------------------------------------------------

    /// <summary>A folder is created with its normalized name under its parent (null = the drive root), with no size and no file.</summary>
    [Fact]
    public void CreateFolder_ReturnsAFolder()
    {
        var result = FileManagerNode.CreateFolder("User@Example.com", 7, "ＤＥＳＩＧＮ", _now);

        Assert.False(result.IsError);
        FileManagerNode folder = result.Value;
        Assert.Equal("user@example.com", folder.AccountEmail.Value);
        Assert.Equal(7, folder.ParentId);
        Assert.Equal("DESIGN", folder.Name);
        Assert.True(folder.IsFolder);
        Assert.Equal(0, folder.Size);
        Assert.Null(folder.StoragePath);
        Assert.False(folder.Uploading);
        Assert.Equal(_now, folder.Created);
        Assert.Equal(_now, folder.Updated);
    }

    /// <summary>A folder name that breaks the name rule is refused.</summary>
    [Fact]
    public void CreateFolder_RefusesAnInvalidName()
    {
        Assert.Equal("FileManager.NameReserved", FileManagerNode.CreateFolder("user@example.com", null, "..", _now).FirstError.Code);
    }

    // -- StartUpload / CompleteUpload ------------------------------------------------

    /// <summary>
    /// Starting an upload creates an "uploading" file: its declared size is reserved against the quota and it is stored under the
    /// GUID name in upload/FileManager, never under the name the user typed.
    /// </summary>
    [Fact]
    public void StartUpload_ReturnsAnUploadingFile_StoredUnderItsGuid()
    {
        var result = FileManagerNode.StartUpload("user@example.com", null, "photo.png", 1234, _storageId, _now, scanDisabled: false);

        Assert.False(result.IsError);
        FileManagerNode file = result.Value;
        Assert.False(file.IsFolder);
        Assert.True(file.Uploading);
        Assert.Equal(1234, file.Size);
        Assert.Equal("photo.png", file.Name);
        Assert.Equal("upload/FileManager/8f1e9c0a-1b2c-4d5e-8f70-112233445566", file.StoragePath);
        Assert.Equal(_storageId, file.StorageId);
    }

    /// <summary>
    /// A file larger than ClamAV can scan is refused when it starts unless the account's virus scan is off; an invalid name is refused
    /// either way.
    /// </summary>
    [Fact]
    public void StartUpload_RefusesAnUnscannableFile_OrAnInvalidName()
    {
        const long unscannable = FileManagerUploadPolicy.MaxScannableBytes + 1;
        Assert.Equal("FileManager.FileTooLarge", FileManagerNode.StartUpload("user@example.com", null, "big.iso", unscannable, _storageId, _now, scanDisabled: false).FirstError.Code);
        Assert.Equal(unscannable, FileManagerNode.StartUpload("user@example.com", null, "big.iso", unscannable, _storageId, _now, scanDisabled: true).Value.Size);
        Assert.Equal("FileManager.NameInvalidCharacter", FileManagerNode.StartUpload("user@example.com", null, "a:b", 1, _storageId, _now, scanDisabled: true).FirstError.Code);
    }

    /// <summary>Completing an upload makes the file a normal one and stamps Updated.</summary>
    [Fact]
    public void CompleteUpload_EndsTheUpload()
    {
        FileManagerNode file = FileManagerNode.StartUpload("user@example.com", null, "photo.png", 1, _storageId, _now, scanDisabled: false).Value;

        file.CompleteUpload(_now.AddMinutes(5));

        Assert.False(file.Uploading);
        Assert.Equal(_now.AddMinutes(5), file.Updated);
        Assert.Equal(_now, file.Created);
    }

    /// <summary>
    /// An image within the preview limit is previewable — also one stored by an account whose scan is off, which leaves no mark on the
    /// file — but not a larger one, a folder or another file.
    /// </summary>
    [Fact]
    public void IsPreviewable_IsTrue_OnlyForAnImageWithinTheLimit()
    {
        FileManagerNode image = FileManagerNode.Reconstitute(1, "user@example.com", null, "a.png", false, 10, "upload/FileManager/x", false, _now, _now);
        FileManagerNode tooLarge = FileManagerNode.Reconstitute(3, "user@example.com", null, "a.png", false, FileManagerPreviewPolicy.MaxPreviewBytes + 1, "upload/FileManager/z", false, _now, _now);

        Assert.True(image.IsPreviewable);
        Assert.False(tooLarge.IsPreviewable);
        Assert.False(File(4).IsPreviewable);
        Assert.False(Folder(5, name: "a.png").IsPreviewable);
    }

    /// <summary>A folder has no storage id; a file stored under another name than a GUID has none either.</summary>
    [Fact]
    public void StorageId_IsNull_WithoutAGuidStorageName()
    {
        Assert.Null(Folder(1).StorageId);
        Assert.Null(File(2).StorageId);
    }

    /// <summary>
    /// An upload whose last activity (<see cref="FileManagerNode.Updated"/>: its start, a chunk, a scan poll) is more than an hour ago is
    /// stale, however long ago it started — a slow upload that keeps sending is never stale. A completed file or a folder never is.
    /// </summary>
    [Fact]
    public void IsStaleUpload_IsTrue_OnlyForAnUnfinishedUploadIdleForOverAnHour()
    {
        FileManagerNode idle = FileManagerNode.Reconstitute(1, "user@example.com", null, "a", false, 1, "upload/FileManager/x", true, _now, _now);
        FileManagerNode active = FileManagerNode.Reconstitute(4, "user@example.com", null, "b", false, 1, "upload/FileManager/y", true, _now.AddHours(-200), _now);

        Assert.True(idle.IsStaleUpload(_now.AddMinutes(61)));
        Assert.False(idle.IsStaleUpload(_now.AddMinutes(59)));
        Assert.False(active.IsStaleUpload(_now.AddMinutes(59)));
        Assert.False(File(2).IsStaleUpload(_now.AddDays(30)));
        Assert.False(Folder(3).IsStaleUpload(_now.AddDays(30)));
    }

    // -- Rename -------------------------------------------------------------------

    /// <summary>Rename stores the normalized new name and stamps Updated.</summary>
    [Fact]
    public void Rename_StoresTheNewName()
    {
        FileManagerNode folder = Folder(1);

        var result = folder.Rename("ｎｅｗ", _now.AddHours(1));

        Assert.False(result.IsError);
        Assert.Equal("new", folder.Name);
        Assert.Equal(_now.AddHours(1), folder.Updated);
    }

    /// <summary>A refused rename leaves the name as it was.</summary>
    [Fact]
    public void Rename_RefusesAnInvalidName_AndChangesNothing()
    {
        FileManagerNode folder = Folder(1);

        Assert.Equal("FileManager.NameTrailingDotOrSpace", folder.Rename("bad.", _now.AddHours(1)).FirstError.Code);
        Assert.Equal("Design", folder.Name);
        Assert.Equal(_now, folder.Updated);
    }

    // -- MoveTo -------------------------------------------------------------------

    /// <summary>A file or folder moves into another folder (or the root) and Updated is stamped.</summary>
    [Fact]
    public void MoveTo_ChangesTheParent()
    {
        FileManagerNode folder = Folder(5, parentId: 1);
        FileManagerNode file = File(6, parentId: 1);

        Assert.False(folder.MoveTo(2, [2, 1], _now.AddHours(1)).IsError);
        Assert.False(file.MoveTo(null, [], _now.AddHours(1)).IsError);

        Assert.Equal(2, folder.ParentId);
        Assert.Null(file.ParentId);
        Assert.Equal(_now.AddHours(1), folder.Updated);
    }

    /// <summary>
    /// A folder cannot be moved into itself or into one of its own sub-folders (a cycle): the target's ancestry (the target and
    /// every folder above it) must not contain the folder.
    /// </summary>
    [Theory]
    [InlineData(5L, new long[] { 5, 1 })]
    [InlineData(9L, new long[] { 9, 7, 5, 1 })]
    public void MoveTo_RefusesACycle(long targetId, long[] targetAncestry)
    {
        FileManagerNode folder = Folder(5, parentId: 1);

        var result = folder.MoveTo(targetId, targetAncestry, _now.AddHours(1));

        Assert.Equal("FileManager.MoveIntoItself", result.FirstError.Code);
        Assert.Equal(1, folder.ParentId);
        Assert.Equal(_now, folder.Updated);
    }

    /// <summary>Reconstitute assigns every stored field verbatim.</summary>
    [Fact]
    public void Reconstitute_AssignsEveryField()
    {
        DateTime created = new(2026, 1, 2);
        DateTime updated = new(2026, 2, 3);

        FileManagerNode node = FileManagerNode.Reconstitute(42, "user@example.com", 3, "n", false, 99, "upload/FileManager/g", true, created, updated);

        Assert.Equal((42L, "user@example.com", (long?)3, "n", false, 99L, "upload/FileManager/g", true, created, updated),
            (node.Id, node.AccountEmail.Value, node.ParentId, node.Name, node.IsFolder, node.Size, node.StoragePath, node.Uploading, node.Created, node.Updated));
    }
}
