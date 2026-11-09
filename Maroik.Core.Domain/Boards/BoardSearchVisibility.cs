namespace Maroik.Core.Domain.Boards;

/// <summary>
/// Which locked posts a board search may return to a viewer, decided by <see cref="Board.SearchVisibilityFor"/>. The search itself
/// runs in SQL, so the domain decides the scope and the repository only translates it into a query.
/// </summary>
public enum BoardSearchVisibility
{
    /// <summary>Only the non-locked matches.</summary>
    PublicOnly,

    /// <summary>The non-locked matches, followed by the viewer's own locked matches.</summary>
    PublicAndOwn,

    /// <summary>Every match, locked or not.</summary>
    AllMatches,

    /// <summary>A viewer who is signed in with a role the rule does not know: the search is not applied, and the plain list is returned.</summary>
    Unsearched
}
