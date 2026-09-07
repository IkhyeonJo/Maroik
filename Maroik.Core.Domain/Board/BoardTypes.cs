namespace Maroik.Core.Domain.Board;

/// <summary>
/// Canonical board-type discriminator strings stored verbatim in <see cref="Board.Type"/>.
/// This is the single source of truth for board-type values across every layer — the service,
/// presentation, and test code all reference these constants directly rather than keeping a
/// parallel enum. Living in the domain keeps board rules such as
/// <see cref="Board.CanBeViewedBy(string?, bool)"/> free of any Contract/Website dependency.
/// </summary>
public static class BoardTypes
{
    /// <summary>Public free-discussion forum. Posts are visible to everyone unless locked.</summary>
    public const string FreeForum = "FreeForum";

    /// <summary>Private note. Visible only to its author — no admin bypass.</summary>
    public const string PrivateNote = "PrivateNote";
}
