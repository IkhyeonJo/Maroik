using Maroik.Core.Domain.FileManagers;
namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>
/// Unit tests for <see cref="FileManagerTree"/>: the questions the drive asks of the whole set of a user's nodes — a folder's ancestry
/// (for a move's cycle check and the breadcrumb), the item count each folder shows, and a node's location path.
/// </summary>
public class FileManagerTreeTests
{
    /// <summary>The fixed time of the nodes.</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A node with the given id, parent and name.</summary>
    private static FileManagerNode Node(long id, long? parentId, string name, bool isFolder = true, bool uploading = false) =>
        FileManagerNode.Reconstitute(id, "user@example.com", parentId, name, isFolder, isFolder ? 0 : 10, isFolder ? null : "upload/FileManager/x", uploading, _now, _now);

    /// <summary>My Drive / Documents(1) / Work(2) / report.txt(3), and My Drive / Design(4) with an image(5) and an unfinished upload(6).</summary>
    private static readonly FileManagerTree _tree = new(
    [
        Node(1, null, "Documents"),
        Node(2, 1, "Work"),
        Node(3, 2, "report.txt", isFolder: false),
        Node(4, null, "Design"),
        Node(5, 4, "logo.png", isFolder: false),
        Node(6, 4, "half.iso", isFolder: false, uploading: true),
    ]);

    /// <summary>A folder's ancestry is the folder itself, then each folder above it up to the root; the root's is empty.</summary>
    [Fact]
    public void Ancestry_ListsTheFolderAndEveryFolderAboveIt()
    {
        Assert.Equal([2L, 1L], _tree.Ancestry(2));
        Assert.Equal([1L], _tree.Ancestry(1));
        Assert.Empty(_tree.Ancestry(null));
    }

    /// <summary>A folder's item count is the number of its direct children, an unfinished upload not counted; the root's too.</summary>
    [Fact]
    public void ItemCount_CountsTheDirectChildren_ThatAreNotUploading()
    {
        Assert.Equal(1, _tree.ItemCount(1));
        Assert.Equal(1, _tree.ItemCount(4));
        Assert.Equal(2, _tree.ItemCount(null));
        Assert.Equal(0, _tree.ItemCount(3));
    }

    /// <summary>A node's location is the names of the folders above it, from the root down.</summary>
    [Fact]
    public void Location_IsTheFolderNamesAboveTheNode()
    {
        Assert.Equal(["Documents", "Work"], _tree.Location(3));
        Assert.Empty(_tree.Location(1));
    }

    /// <summary>Every node under a folder, at any depth (what a folder delete removes and a folder download fetches).</summary>
    [Fact]
    public void Descendants_ListsEveryNodeUnderTheFolder()
    {
        Assert.Equal([2L, 3L], _tree.Descendants(1).Select(n => n.Id).Order());
        Assert.Empty(_tree.Descendants(3));
    }

    /// <summary>A node that is not in the tree has no ancestry (it belongs to another user, or is gone).</summary>
    [Fact]
    public void Contains_IsFalse_ForAnUnknownNode()
    {
        Assert.True(_tree.Contains(5));
        Assert.False(_tree.Contains(99));
        Assert.Null(_tree.Find(99));
        Assert.Equal("logo.png", _tree.Find(5)?.Name);
    }
}
