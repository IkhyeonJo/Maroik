using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Service.Constants;

/// <summary>The board kinds, for the code outside the Service layer that may not read the Domain; each value is the Domain's own constant.</summary>
public static class BoardKinds
{
    /// <summary><see cref="BoardTypes.FreeForum"/>.</summary>
    public const string FreeForum = BoardTypes.FreeForum;

    /// <summary><see cref="BoardTypes.PrivateNote"/>.</summary>
    public const string PrivateNote = BoardTypes.PrivateNote;
}
