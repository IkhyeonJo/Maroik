using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardService
{
    Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile);
    Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile);
    Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin);
    Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request);
    Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin);
    Task<ServiceResult> IncrementBoardViewAsync(long boardId);
}
