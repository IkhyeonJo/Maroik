using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardService
{
    Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type);
    Task<BoardResponse?> GetBoardByIdAsync(long id);
    Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId);
    Task<Dictionary<long, BoardAttachedFileResponse?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds);
    Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId);
    Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds);
    Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile);
    Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile);
    Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin);
    Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request, string? requiredOwnerNickname = null);
    Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin);
    Task<ServiceResult> IncrementBoardViewAsync(long boardId);
    Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string area, string boardType);
    Task<byte[]?> DownloadFileAsync(string filePath);
}
