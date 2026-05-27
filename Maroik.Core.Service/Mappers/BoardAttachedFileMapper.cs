using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

/// <summary>Maps <see cref="BoardAttachedFile"/> domain objects to <see cref="BoardAttachedFileResponse"/> DTOs.</summary>
internal static class BoardAttachedFileMapper
{
    /// <summary>Converts a <see cref="BoardAttachedFile"/> domain object to its corresponding <see cref="BoardAttachedFileResponse"/> DTO.</summary>
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
