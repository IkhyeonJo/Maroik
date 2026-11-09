using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>What an upload may replace, and a "keep both" name for a name that is nearly all extension.</summary>
public class FileManagerOverwriteAndLongNameTests
{
    /// <summary>The fixed time of the nodes.</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A file can be overwritten; a folder cannot.</summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CanBeOverwritten_IsForFilesOnly(bool isFolder, bool expected)
    {
        FileManagerNode node = FileManagerNode.Reconstitute(1, "user@example.com", null, "x", isFolder, 0, null, false, _now, _now);

        Assert.Equal(expected, node.CanBeOverwritten);
    }

    /// <summary>
    /// Regression: an extension too long to leave room for the stem threw (a negative length). The whole name is cut instead, and the
    /// result stays a valid, free name of at most the limit.
    /// </summary>
    [Theory]
    [InlineData(253)]
    [InlineData(251)]
    public void KeepBothName_HandlesAnExtensionThatLeavesNoRoomForTheStem(int extensionLength)
    {
        string name = "a." + new string('b', extensionLength);

        string result = FileManagerNamePolicy.KeepBothName(name, [name]);

        Assert.Equal(ShortTextPolicy.MaxLength, result.Length);
        Assert.EndsWith(" (1)", result, StringComparison.Ordinal);
        Assert.False(FileManagerNamePolicy.Validate(result).IsError);
    }
}
