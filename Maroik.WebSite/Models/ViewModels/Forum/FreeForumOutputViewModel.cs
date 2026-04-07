using Maroik.Core.Contract.Dtos;

namespace Maroik.WebSite.Models.ViewModels.Forum;

public class FreeForumOutputViewModel
{
    public string Method { get; set; }

    public Pager Pager { get; set; }

    public IEnumerable<BoardResponse> NoticeBoards { get; set; } = [];

    public IEnumerable<BoardResponse> Boards { get; set; } = [];

    public BoardOutputViewModel BoardOutputViewModel { get; set; }

    public int DetailBoardId { get; set; }

    public int DetailCurrentPage { get; set; }

    public int EditBoardId { get; set; }

    public int EditCurrentPage { get; set; }

    // Populated by controller - consumed by view
    public Maroik.Core.Contract.Dtos.AccountResponse LoginedAccount { get; set; } = new();
    public string LoginedAccountTimeZoneIanaId { get; set; } = "";
    public List<Maroik.Core.Contract.Dtos.AccountResponse> AllAccounts { get; set; } = [];
    public Dictionary<long, int> CommentCountByBoardId { get; set; } = new();
    public Dictionary<long, List<Maroik.Core.Contract.Dtos.BoardAttachedFileResponse>> AttachedFilesByBoardId { get; set; } = new();
    public List<Maroik.Core.Contract.Dtos.BoardCommentResponse> DetailBoardComments { get; set; } = [];
    public string SelectedSearchType { get; set; } = "";
    public string TypedSearchText { get; set; } = "";

}