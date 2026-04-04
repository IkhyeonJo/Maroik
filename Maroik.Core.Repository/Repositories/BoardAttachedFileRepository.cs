using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class BoardAttachedFileRepository : IBoardAttachedFileRepository
{
    private readonly ApplicationDbContext _context;

    public BoardAttachedFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BoardAttachedFileResponse>> GetAllAsync()
    {
        return (await _context.BoardAttachedFiles.ToListAsync()).Adapt<List<BoardAttachedFileResponse>>();
    }

    public async Task SaveBoardAttachedFileAsync(BoardAttachedFileRequest request)
    {
        _ = await _context.BoardAttachedFiles.AddAsync(request.Adapt<BoardAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateBoardAttachedFileAsync(BoardAttachedFileRequest request)
    {
        _ = _context.BoardAttachedFiles.Update(request.Adapt<BoardAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }
}
