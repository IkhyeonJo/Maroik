using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IBoardPolicyService"/>
public sealed class BoardPolicyService : IBoardPolicyService
{
    /// <inheritdoc />
    public bool IsContentOwner(AccountResponse viewer, string? contentWriter) => Board.IsOwnedBy(contentWriter, viewer.Nickname);

    /// <inheritdoc />
    public bool IsContentOwnerOrAdmin(AccountResponse viewer, string? contentWriter) =>
        Board.IsOwnedBy(contentWriter, viewer.Nickname) || viewer.Role == Role.Admin;

    /// <inheritdoc />
    public bool IsTitleVisibleToOwnerOrAdmin(BoardResponse board, AccountResponse viewer) =>
        Board.CanBeViewedBy(board.Type, board.Deleted, board.Locked, board.Writer, viewer.Nickname, viewer.Role == Role.Admin);

    /// <inheritdoc />
    public bool IsTitleVisibleToOwner(BoardResponse board, AccountResponse viewer) =>
        Board.CanBeViewedBy(board.Type, board.Deleted, board.Locked, board.Writer, viewer.Nickname, viewerIsAdmin: false);
}
