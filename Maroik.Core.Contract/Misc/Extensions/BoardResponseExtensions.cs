using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Misc.Extensions;

/// <summary>
/// Extension methods encoding the "can this viewer see a locked post's title" rule for list views.
/// An unlocked post's title is always visible; a locked post's title is redacted
/// unless the viewer passes the board's ownership check. FreeForum posts grant
/// visibility to the writer or any Admin; PrivateNote posts are stricter and
/// grant visibility to the writer only (see <c>Management.PrivateNote.cshtml</c>).
/// UI mirror only — the authoritative gate on opening a post is <c>Board.CanBeViewedBy</c>
/// (Maroik.Core.Domain), invoked server-side via <c>IBoardService.CanView</c>.
/// </summary>
public static class BoardResponseExtensions
{
    extension(BoardResponse board)
    {
        /// <summary>True when the post is unlocked, or the viewer is the writer or an Admin.</summary>
        public bool IsTitleVisibleToOwnerOrAdmin(AccountResponse viewer) =>
            !board.Locked || viewer.IsOwnerOrAdmin(board.Writer);

        /// <summary>True when the post is unlocked, or the viewer is the writer.</summary>
        public bool IsTitleVisibleToOwner(AccountResponse viewer) =>
            !board.Locked || viewer.IsOwner(board.Writer);
    }
}
