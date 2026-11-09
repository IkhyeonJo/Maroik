using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The post rules the Website needs for what it shows; the services enforce them again on every write.</summary>
public interface IBoardPolicyService
{
    /// <summary>Whether the viewer wrote the content.</summary>
    bool IsContentOwner(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether the viewer wrote the content or is an administrator.</summary>
    bool IsContentOwnerOrAdmin(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether the write form offers the viewer the option to pin the post as a notice.</summary>
    bool CanPostNotice(AccountResponse viewer);

    /// <summary>Whether the detail panel offers the viewer the comment form.</summary>
    bool CanWriteComment(AccountResponse viewer);

    /// <summary>Whether the edit page opens for the viewer.</summary>
    bool CanOpenEditPage(BoardResponse board, AccountResponse viewer);

    /// <summary>Whether a post's title may be shown to the viewer, an administrator seeing it too.</summary>
    bool IsTitleVisibleToOwnerOrAdmin(BoardResponse board, AccountResponse viewer);

    /// <summary>Whether a post's title may be shown to the viewer, with no administrator bypass.</summary>
    bool IsTitleVisibleToOwner(BoardResponse board, AccountResponse viewer);
}
