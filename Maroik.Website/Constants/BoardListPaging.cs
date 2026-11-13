namespace Maroik.Website.Constants;

/// <summary>
/// How the board lists (FreeForum, PrivateNote) are paged. A presentation setting, not a business rule, so it lives here and not in
/// the domain (like <see cref="CulturePolicy"/>).
/// </summary>
public static class BoardListPaging
{
    /// <summary>How many posts a list page shows.</summary>
    public const int PageSize = 5;

    /// <summary>The page to show for a requested one: there is no page before the first.</summary>
    public static int PageOf(int requestedPage) => Math.Max(requestedPage, 1);
}
