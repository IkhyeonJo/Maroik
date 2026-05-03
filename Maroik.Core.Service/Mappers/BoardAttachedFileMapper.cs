using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class BoardAttachedFileMapper
{
    internal static BoardAttachedFileResponse ToResponse(BoardAttachedFile file) => new()
    {
        Id = file.Id,
        BoardId = file.BoardId,
        Size = file.Size,
        Name = file.Name,
        Extension = file.Extension,
        Path = file.Path
    };
}
