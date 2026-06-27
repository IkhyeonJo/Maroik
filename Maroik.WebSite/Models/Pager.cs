namespace Maroik.WebSite.Models;

/// <summary>
/// Pagination helper used to calculate the current page window for list views (e.g. board post lists).
/// Pass an instance to the view and use <see cref="StartPage"/>/<see cref="EndPage"/> to render page links.
/// </summary>
public class Pager
{
    /// <summary>Total number of items across all pages.</summary>
    public int TotalItems { get; private set; }

    /// <summary>The currently active page number (1-based).</summary>
    public int CurrentPage { get; private set; }

    /// <summary>Number of items displayed per page.</summary>
    public int PageSize { get; private set; }

    /// <summary>Total number of pages calculated from <see cref="TotalItems"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages { get; private set; }

    /// <summary>First page number shown in the page-link range (at most 5 links are displayed at once).</summary>
    public int StartPage { get; private set; }

    /// <summary>Last page number shown in the page-link range.</summary>
    public int EndPage { get; private set; }

    /// <summary>Parameterless constructor required for model binding.</summary>
    public Pager()
    {
    }

    /// <summary>
    /// Calculates pagination values for the given total item count and current page.
    /// The visible page-link window always shows at most 5 consecutive page numbers
    /// centred around <paramref name="page"/>.
    /// </summary>
    /// <param name="totalItems">Total number of records to paginate.</param>
    /// <param name="page">The currently requested page number (1-based).</param>
    /// <param name="pageSize">Number of records per page (default: 10).</param>
    public Pager(int totalItems, int page, int pageSize = 10)
    {
        int totalPages = (int)Math.Ceiling(totalItems / (decimal)pageSize);
        int currentPage = page;

        // Build a window of ±2 pages around the current page
        int startPage = currentPage - 2;
        int endPage = currentPage + 2;

        // Clamp the window to page 1 at the left boundary
        if (startPage <= 0)
        {
            endPage -= startPage - 1;
            startPage = 1;
        }

        // Clamp the window to the last page at the right boundary
        if (endPage > totalPages)
        {
            endPage = totalPages;

            // Keep the window at 5 links wide when there are enough pages
            if (endPage > 5)
            {
                startPage = endPage - 4;
            }
        }

        TotalItems = totalItems;
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalPages = totalPages;
        StartPage = startPage;
        EndPage = endPage;
    }
}
