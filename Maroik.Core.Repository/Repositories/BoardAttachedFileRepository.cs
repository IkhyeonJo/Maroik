using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IBoardAttachedFileRepository"/>.</summary>
public class BoardAttachedFileRepository : IBoardAttachedFileRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="BoardAttachedFileRepository"/> with <paramref name="context"/>.</summary>
    public BoardAttachedFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<BoardAttachedFileResponse>> GetAllAsync()
    {
        return (await _context.BoardAttachedFiles.ToListAsync()).Adapt<List<BoardAttachedFileResponse>>();
    }

    /// <inheritdoc />
    public async Task<BoardAttachedFileResponse?> GetByBoardIdAsync(long boardId)
    {
        BoardAttachedFile? file = await _context.BoardAttachedFiles.FirstOrDefaultAsync(x => x.BoardId == boardId);
        return file?.Adapt<BoardAttachedFileResponse>();
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFileResponse?>> GetByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();
        List<BoardAttachedFile> files = await _context.BoardAttachedFiles
            .Where(x => ids.Contains(x.BoardId))
            .ToListAsync();
        return ids.ToDictionary(id => id, id => files.FirstOrDefault(f => f.BoardId == id)?.Adapt<BoardAttachedFileResponse>());
    }

    /// <inheritdoc />
    public async Task SaveBoardAttachedFileAsync(BoardAttachedFileRequest request)
    {
        _ = await _context.BoardAttachedFiles.AddAsync(request.Adapt<BoardAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateBoardAttachedFileAsync(BoardAttachedFileRequest request)
    {
        _ = _context.BoardAttachedFiles.Update(request.Adapt<BoardAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }
}
