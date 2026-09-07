namespace Maroik.Core.Domain.Board;

/// <summary>
/// Canonical board-type discriminator strings stored verbatim in <see cref="Board.Type"/>.
/// The presentation layer has its own <c>BoardType</c> enum (with <c>[Description]</c> values that
/// match these strings); the domain keeps its own copy so board rules such as
/// <see cref="Board.CanBeViewedBy(string?, bool)"/> do not depend on a Contract/Website type.
/// </summary>
public static class BoardTypes
{
    /// <summary>Public free-discussion forum. Posts are visible to everyone unless locked.</summary>
    public const string FreeForum = "FreeForum";

    /// <summary>Private note. Visible only to its author — no admin bypass.</summary>
    public const string PrivateNote = "PrivateNote";
}
