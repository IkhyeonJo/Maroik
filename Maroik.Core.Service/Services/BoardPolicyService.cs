using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IBoardPolicyService"/>
public sealed class BoardPolicyService : IBoardPolicyService
{
    /// <inheritdoc />
    public bool IsContentOwner(AccountResponse viewer, string? contentWriter) => Board.IsOwnedBy(contentWriter, viewer.Nickname);

    /// <inheritdoc />
    public bool IsContentOwnerOrAdmin(AccountResponse viewer, string? contentWriter) =>
        Board.IsOwnedByOrAdmin(contentWriter, viewer.Nickname, viewer.Role == Role.Admin);

    /// <inheritdoc />
    public bool CanPostNotice(AccountResponse viewer) => Board.CanBePinnedBy(viewer.Role == Role.Admin);

    /// <inheritdoc />
    public bool CanWriteComment(AccountResponse viewer) => Board.AcceptsCommentsFrom(viewer.Role);

    /// <inheritdoc />
    public bool CanOpenEditPage(BoardResponse board, AccountResponse viewer) => Board.EditPageOpensFor(board.Deleted, board.Writer, viewer.Nickname);

    /// <inheritdoc />
    public bool IsTitleVisibleToOwnerOrAdmin(BoardResponse board, AccountResponse viewer) =>
        Board.CanBeViewedBy(board.Type, board.Deleted, board.Locked, board.Writer, viewer.Nickname, viewer.Role == Role.Admin);

    /// <inheritdoc />
    public bool IsTitleVisibleToOwner(BoardResponse board, AccountResponse viewer) =>
        Board.CanBeViewedBy(board.Type, board.Deleted, board.Locked, board.Writer, viewer.Nickname, viewerIsAdmin: false);
}
