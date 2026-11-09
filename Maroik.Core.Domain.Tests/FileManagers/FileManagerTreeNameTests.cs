using Maroik.Core.Domain.FileManagers;

namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>
/// The drive's rules over several nodes: names are unique within a folder among finished items (an unfinished upload holds no name
/// yet), and every file counts against the quota, unfinished uploads included.
/// </summary>
public class FileManagerTreeNameTests
{
    /// <summary>The fixed time of the nodes.</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A node with the given id, parent and name.</summary>
    private static FileManagerNode Node(long id, long? parentId, string name, bool isFolder = false, bool uploading = false) =>
        FileManagerNode.Reconstitute(id, "user@example.com", parentId, name, isFolder, isFolder ? 0 : 10, isFolder ? null : "upload/FileManager/x", uploading, _now, _now);

    /// <summary>My Drive holds Docs(1, a folder), a.txt(2) and an unfinished b.txt(3); Docs holds a.txt(4) and c.txt(5).</summary>
    private static readonly FileManagerTree _tree = new(
    [
        Node(1, null, "Docs", isFolder: true),
        Node(2, null, "a.txt"),
        Node(3, null, "b.txt", uploading: true),
        Node(4, 1, "a.txt"),
        Node(5, 1, "c.txt"),
    ]);

    /// <summary>The root and a folder can hold items; a file or a node of no drive cannot.</summary>
    [Fact]
    public void IsFolderOrRoot_AcceptsTheRootAndFoldersOnly()
    {
        Assert.True(_tree.IsFolderOrRoot(null));
        Assert.True(_tree.IsFolderOrRoot(1));
        Assert.False(_tree.IsFolderOrRoot(2));
        Assert.False(_tree.IsFolderOrRoot(99));
    }

    /// <summary>A finished node is found; an unfinished upload and an unknown id are not.</summary>
    [Fact]
    public void FindFinished_SkipsUnfinishedUploads()
    {
        Assert.Equal(2, _tree.FindFinished(2)?.Id);
        Assert.Null(_tree.FindFinished(3));
        Assert.Null(_tree.FindFinished(99));
    }

    /// <summary>A folder's finished items leave out its unfinished uploads.</summary>
    [Fact]
    public void FinishedChildren_LeavesOutUnfinishedUploads()
    {
        Assert.Equal([1L, 2L], _tree.FinishedChildren(null).Select(n => n.Id).Order());
    }

    /// <summary>A name is looked up exactly and within its own folder, and an unfinished upload holds no name.</summary>
    [Fact]
    public void FindFinishedNamed_LooksInThatFolderOnly_AndExactly()
    {
        Assert.Equal(2, _tree.FindFinishedNamed(null, "a.txt")?.Id);
        Assert.Equal(4, _tree.FindFinishedNamed(1, "a.txt")?.Id);
        Assert.Null(_tree.FindFinishedNamed(null, "A.TXT"));
        Assert.Null(_tree.FindFinishedNamed(null, "b.txt"));
    }

    /// <summary>A node does not conflict with itself, only with another item of that name.</summary>
    [Fact]
    public void IsNameTakenByAnother_IgnoresTheNodeItself()
    {
        Assert.False(_tree.IsNameTakenByAnother(null, "a.txt", 2));
        Assert.True(_tree.IsNameTakenByAnother(null, "Docs", 2));
        Assert.False(_tree.IsNameTakenByAnother(null, "free.txt", 2));
    }

    /// <summary>A moved item conflicts with what stays in the target, not with itself.</summary>
    [Fact]
    public void NameConflictAfterMoving_FindsANameThatStaysInTheTarget()
    {
        Assert.Equal("a.txt", _tree.NameConflictAfterMoving([_tree.Find(2)!], 1));
        Assert.Null(_tree.NameConflictAfterMoving([_tree.Find(4)!], 1));
        Assert.Null(_tree.NameConflictAfterMoving([_tree.Find(5)!], null));
    }

    /// <summary>Two moved items of one name conflict with each other, even in an empty target.</summary>
    [Fact]
    public void NameConflictAfterMoving_FindsTwoMovedItemsOfOneName()
    {
        FileManagerTree tree = new([Node(1, null, "Empty", isFolder: true), Node(2, null, "x.txt"), Node(3, 9, "x.txt")]);

        Assert.Equal("x.txt", tree.NameConflictAfterMoving([tree.Find(2)!, tree.Find(3)!], 1));
    }

    /// <summary>Every file counts, an unfinished upload too; a folder counts nothing.</summary>
    [Fact]
    public void UsedBytes_CountsEveryFile_IncludingUnfinishedUploads()
    {
        Assert.Equal(40, FileManagerTree.UsedBytes(_tree.Nodes));
    }
}
