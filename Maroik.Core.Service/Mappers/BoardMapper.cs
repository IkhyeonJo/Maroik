using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class BoardMapper
{
    internal static BoardResponse ToResponse(Board board) => new()
    {
        Id = board.Id,
        Type = board.Type,
        Title = board.Title,
        Content = board.Content,
        Writer = board.Writer,
        Created = board.Created,
        Updated = board.Updated,
        View = board.View,
        Deleted = board.Deleted,
        Locked = board.Locked,
        Noticed = board.Noticed
    };
}
