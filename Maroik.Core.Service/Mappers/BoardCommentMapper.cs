using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class BoardCommentMapper
{
    internal static BoardCommentResponse ToResponse(BoardComment comment) => new()
    {
        Id = comment.Id,
        BoardId = comment.BoardId,
        Order = comment.Order,
        AvatarImagePath = comment.AvatarImagePath,
        Writer = comment.Writer,
        Content = comment.Content,
        Created = comment.Created,
        Deleted = comment.Deleted
    };
}
