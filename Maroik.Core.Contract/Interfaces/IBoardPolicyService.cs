using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The post rules the Website needs for what it shows; the services enforce them again on every write.</summary>
public interface IBoardPolicyService
{
    /// <summary>Whether the viewer wrote the content.</summary>
    bool IsContentOwner(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether the viewer wrote the content or is an administrator.</summary>
    bool IsContentOwnerOrAdmin(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether a post's title may be shown to the viewer, an administrator seeing it too.</summary>
    bool IsTitleVisibleToOwnerOrAdmin(BoardResponse board, AccountResponse viewer);

    /// <summary>Whether a post's title may be shown to the viewer, with no administrator bypass.</summary>
    bool IsTitleVisibleToOwner(BoardResponse board, AccountResponse viewer);
}
